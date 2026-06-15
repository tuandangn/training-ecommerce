using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;

namespace NamEcommerce.Web.Services.DeliveryNotes;

public sealed class DeliveryRunModelFactory(
    IDeliveryRunAppService deliveryRunAppService,
    IDeliveryNoteAppService deliveryNoteAppService,
    IUserAppService userAppService,
    AppConfig appConfig) : IDeliveryRunModelFactory
{
    public async Task<DeliveryRunListModel> PrepareDeliveryRunListModelAsync(DeliveryRunSearchModel searchModel)
    {
        var pageNumber = searchModel.PageNumber ?? 1;
        var pageSize = searchModel.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0 || !appConfig.PageSizeOptions.Contains(pageSize)) pageSize = appConfig.DefaultPageSize;

        var pagedData = await deliveryRunAppService.GetListAsync(
            pageNumber - 1,
            pageSize,
            searchModel.Keywords,
            searchModel.AssignedDeliveryUserId,
            searchModel.Status).ConfigureAwait(false);

        return new DeliveryRunListModel
        {
            Keywords = searchModel.Keywords,
            AssignedDeliveryUserId = searchModel.AssignedDeliveryUserId,
            Status = searchModel.Status,
            AvailableDeliveryUsers = await PrepareDeliveryUserOptionsAsync().ConfigureAwait(false),
            Data = PagedDataModel.Create(
                await PrepareDeliveryRunListItemsAsync(pagedData.Items).ConfigureAwait(false),
                pagedData.Pagination.PageIndex,
                pagedData.Pagination.PageSize,
                pagedData.Pagination.TotalCount)
        };
    }

    public async Task<CreateDeliveryRunModel> PrepareCreateDeliveryRunModelAsync(CreateDeliveryRunModel? oldModel = null)
    {
        var model = oldModel ?? new CreateDeliveryRunModel();
        model.AvailableDeliveryUsers = await PrepareDeliveryUserOptionsAsync().ConfigureAwait(false);
        model.AvailableDeliveryNotes = await PrepareCandidateDeliveryNotesAsync().ConfigureAwait(false);
        return model;
    }

    public async Task<DeliveryRunDetailsModel> PrepareDeliveryRunDetailsModelAsync(Guid id)
    {
        var run = await deliveryRunAppService.GetByIdAsync(id).ConfigureAwait(false)
                  ?? throw new ArgumentException("Delivery run not found");
        var currentNotes = new Dictionary<Guid, DeliveryNoteAppDto>();
        foreach (var item in run.Items)
        {
            var note = await deliveryNoteAppService.GetByIdAsync(item.DeliveryNoteId).ConfigureAwait(false);
            if (note is not null)
                currentNotes[item.DeliveryNoteId] = note;
        }

        var itemModels = run.Items.Select(item =>
        {
            currentNotes.TryGetValue(item.DeliveryNoteId, out var note);

            return new DeliveryRunItemModel
            {
                DeliveryNoteStatus = note?.Status,
                DeliveredOnUtc = note?.DeliveredOnUtc,
                Id = item.Id,
                DeliveryNoteId = item.DeliveryNoteId,
                DeliveryNoteCode = item.DeliveryNoteCode,
                OrderCode = note?.OrderCode ?? item.OrderCode,
                CustomerName = note?.CustomerName ?? item.CustomerName,
                CustomerPhone = string.IsNullOrWhiteSpace(note?.ShippingPhoneNumber)
                    ? item.ShippingPhoneNumber
                    : note?.ShippingPhoneNumber,
                ShippingPhoneNumber = string.IsNullOrWhiteSpace(note?.ShippingPhoneNumber)
                    ? item.ShippingPhoneNumber
                    : note?.ShippingPhoneNumber,
                ShippingAddress = note?.ShippingAddress ?? item.ShippingAddress,
                AmountToCollect = note?.AmountToCollect ?? item.AmountToCollect,
                CashCollectedAmount = note?.DeliveryCashCollectedAmount,
                ReceiverName = note?.DeliveryReceiverName,
                DeliveryProofPictureId = note?.DeliveryProofPictureId,
                ProductItems = note?.Items.Select(product => new DeliveryRunProductItemModel
                {
                    DeliveryNoteItemId = product.Id,
                    ProductName = product.ProductName,
                    Quantity = product.Quantity,
                    UnitPrice = product.UnitPrice,
                    SubTotal = product.SubTotal,
                    QuantityDecimalPlaces = 2
                }).ToList() ?? []
            };
        }).ToList();
        var progress = GetProgressInfo((DeliveryRunStatus)run.Status, itemModels);

        return new DeliveryRunDetailsModel
        {
            Id = run.Id,
            Code = run.Code,
            AssignedDeliveryUserId = run.AssignedDeliveryUserId,
            AssignedDeliveryUsername = run.AssignedDeliveryUsername,
            AssignedDeliveryFullName = run.AssignedDeliveryFullName,
            Status = run.Status,
            StatusName = progress.StatusName,
            DeliveredItemCount = progress.DeliveredItemCount,
            IsDeliveryCompleted = progress.IsCompleted,
            PreparedByUserId = run.PreparedByUserId,
            PreparedOnUtc = run.PreparedOnUtc,
            HandedOverByUserId = run.HandedOverByUserId,
            HandedOverOnUtc = run.HandedOverOnUtc,
            DriverCachedOnUtc = run.DriverCachedOnUtc,
            DriverCacheDeviceId = run.DriverCacheDeviceId,
            PaperManifestIssued = run.PaperManifestIssued,
            PaperManifestIssuedOnUtc = run.PaperManifestIssuedOnUtc,
            CashHandoverConfirmedByUserId = run.CashHandoverConfirmedByUserId,
            CashHandoverConfirmedByUsername = run.CashHandoverConfirmedByUsername,
            CashHandoverConfirmedByFullName = run.CashHandoverConfirmedByFullName,
            CashHandoverConfirmedOnUtc = run.CashHandoverConfirmedOnUtc,
            CashHandoverAmount = run.CashHandoverAmount,
            CashHandoverNote = run.CashHandoverNote,
            Note = run.Note,
            CreatedOnUtc = run.CreatedOnUtc,
            UpdatedOnUtc = run.UpdatedOnUtc,
            Items = itemModels
        };
    }

    public async Task<DeliveryMobileIndexModel> PrepareDeliveryMobileIndexModelAsync(Guid currentUserId, string currentUserFullName,
        bool showCompleted)
    {
        var pagedData = await deliveryRunAppService.GetListAsync(
            0,
            50,
            null,
            currentUserId,
            null).ConfigureAwait(false);

        var candidateRuns = pagedData.Items
            .Where(run => run.Status != (int)DeliveryRunStatus.Cancelled)
            .Where(run => run.Status != (int)DeliveryRunStatus.Closed)
            .ToList();
        var runs = new List<DeliveryMobileRunListItemModel>();
        foreach (var run in candidateRuns)
        {
            var details = await PrepareDeliveryRunDetailsModelAsync(run.Id).ConfigureAwait(false);
            var model = ToMobileListModel(run, details.Items);
            if (showCompleted != model.IsDeliveryCompleted)
                continue;

            runs.Add(model);
        }

        return new DeliveryMobileIndexModel
        {
            CurrentUserId = currentUserId,
            CurrentUserFullName = currentUserFullName,
            ShowCompleted = showCompleted,
            Runs = runs
        };
    }

    public async Task<DeliveryMobileRunModel> PrepareDeliveryMobileRunModelAsync(Guid id, Guid currentUserId, string currentUserFullName)
    {
        var run = await PrepareDeliveryRunDetailsModelAsync(id).ConfigureAwait(false);
        if (run.AssignedDeliveryUserId != currentUserId)
            throw new UnauthorizedAccessException("Delivery run is not assigned to current user");

        return new DeliveryMobileRunModel
        {
            CurrentUserId = currentUserId,
            CurrentUserFullName = currentUserFullName,
            Run = run
        };
    }

    private async Task<EntityOptionListModel> PrepareDeliveryUserOptionsAsync()
    {
        var users = await userAppService.GetUsersByRoleAsync(SystemUserRoleNames.DeliveryStaff).ConfigureAwait(false);
        return new EntityOptionListModel
        {
            Options = users.Select(user => new EntityOptionListModel.EntityOptionModel
            {
                Id = user.Id,
                Name = string.IsNullOrWhiteSpace(user.FullName)
                    ? user.Username
                    : $"{user.FullName} ({user.Username})"
            }).ToList()
        };
    }

    private async Task<IList<DeliveryRunCandidateDeliveryNoteModel>> PrepareCandidateDeliveryNotesAsync()
    {
        var notes = await deliveryNoteAppService.GetListAsync(null, 0, 500).ConfigureAwait(false);
        var activeDeliveryNoteIds = await GetActiveDeliveryRunNoteIdsAsync().ConfigureAwait(false);

        return notes.Items
            .Where(note => note.Status == (int)DeliveryNoteStatus.Confirmed)
            .Where(note => !note.IsDirectShip && note.SourceType == (int)DeliveryNoteSourceType.ToCustomer)
            .Where(note => !activeDeliveryNoteIds.Contains(note.Id))
            .OrderBy(note => note.CreatedOnUtc)
            .Select(note => new DeliveryRunCandidateDeliveryNoteModel
            {
                Id = note.Id,
                Code = note.Code,
                OrderCode = note.OrderCode,
                CustomerName = note.CustomerName,
                ShippingPhoneNumber = string.IsNullOrWhiteSpace(note.ShippingPhoneNumber)
                    ? note.CustomerPhone
                    : note.ShippingPhoneNumber,
                ShippingAddress = note.ShippingAddress,
                AssignedDeliveryFullName = note.AssignedDeliveryFullName,
                ProductSummary = BuildProductSummary(note.Items),
                TotalQuantity = note.Items.Sum(item => item.Quantity),
                AmountToCollect = note.AmountToCollect,
                CreatedOnUtc = note.CreatedOnUtc
            })
            .ToList();
    }

    private async Task<HashSet<Guid>> GetActiveDeliveryRunNoteIdsAsync()
    {
        var runs = await deliveryRunAppService.GetListAsync(0, 500, null, null, null).ConfigureAwait(false);
        var activeRuns = runs.Items
            .Where(run => run.Status != (int)DeliveryRunStatus.Closed && run.Status != (int)DeliveryRunStatus.Cancelled)
            .ToList();

        var noteIds = new HashSet<Guid>();
        foreach (var run in activeRuns)
        {
            var details = await deliveryRunAppService.GetByIdAsync(run.Id).ConfigureAwait(false);
            if (details is null)
                continue;

            foreach (var item in details.Items)
                noteIds.Add(item.DeliveryNoteId);
        }

        return noteIds;
    }

    private static string BuildProductSummary(IEnumerable<DeliveryNoteItemAppDto> items)
    {
        var itemList = items.ToList();
        var summary = string.Join(", ", itemList
            .Take(2)
            .Select(item => $"{item.ProductName} x {item.Quantity:#,##0.##}"));
        if (itemList.Count <= 2)
            return summary;

        return $"{summary} +{itemList.Count - 2}";
    }

    private async Task<IList<DeliveryRunListItemModel>> PrepareDeliveryRunListItemsAsync(IEnumerable<DeliveryRunListAppDto> runs)
    {
        var items = new List<DeliveryRunListItemModel>();
        foreach (var run in runs)
        {
            var details = await PrepareDeliveryRunDetailsModelAsync(run.Id).ConfigureAwait(false);
            items.Add(ToListModel(run, details.Items));
        }

        return items;
    }

    private static DeliveryRunListItemModel ToListModel(DeliveryRunListAppDto run, IList<DeliveryRunItemModel> items)
    {
        var progress = GetProgressInfo((DeliveryRunStatus)run.Status, items);

        return new()
        {
            Id = run.Id,
            Code = run.Code,
            AssignedDeliveryUserId = run.AssignedDeliveryUserId,
            AssignedDeliveryFullName = run.AssignedDeliveryFullName,
            Status = run.Status,
            StatusName = progress.StatusName,
            ItemCount = run.ItemCount,
            DeliveredItemCount = progress.DeliveredItemCount,
            IsDeliveryCompleted = progress.IsCompleted,
            AmountToCollect = run.AmountToCollect,
            DriverCachedOnUtc = run.DriverCachedOnUtc,
            PaperManifestIssued = run.PaperManifestIssued,
            HandedOverOnUtc = run.HandedOverOnUtc,
            CashHandoverConfirmedOnUtc = run.CashHandoverConfirmedOnUtc,
            CashHandoverAmount = run.CashHandoverAmount,
            CreatedOnUtc = run.CreatedOnUtc
        };
    }

    private static DeliveryMobileRunListItemModel ToMobileListModel(DeliveryRunListAppDto run, IList<DeliveryRunItemModel> items)
    {
        var progress = GetProgressInfo((DeliveryRunStatus)run.Status, items);

        return new()
        {
            Id = run.Id,
            Code = run.Code,
            Status = run.Status,
            StatusName = progress.StatusName,
            ItemCount = run.ItemCount,
            DeliveredItemCount = progress.DeliveredItemCount,
            IsDeliveryCompleted = progress.IsCompleted,
            AmountToCollect = GetPendingCashAmount(items),
            DriverCachedOnUtc = run.DriverCachedOnUtc,
            PaperManifestIssued = run.PaperManifestIssued,
            HandedOverOnUtc = run.HandedOverOnUtc,
            CreatedOnUtc = run.CreatedOnUtc,
            Items = items
        };
    }

    private static DeliveryRunProgressInfo GetProgressInfo(DeliveryRunStatus status, IList<DeliveryRunItemModel> items)
    {
        var deliveredCount = items.Count(item => item.DeliveryNoteStatus == (int)DeliveryNoteStatus.Delivered);
        var openCount = items.Count(item =>
            item.DeliveryNoteStatus != (int)DeliveryNoteStatus.Delivered &&
            item.DeliveryNoteStatus != (int)DeliveryNoteStatus.Cancelled);
        var isCompleted = items.Count > 0 && openCount == 0;
        var hasStarted = deliveredCount > 0 ||
            items.Any(item => item.DeliveryNoteStatus == (int)DeliveryNoteStatus.Delivering);
        var statusName = status switch
        {
            DeliveryRunStatus.Cancelled => "Đã hủy",
            _ when isCompleted => "Đã giao xong",
            _ when hasStarted || status == DeliveryRunStatus.HandedToDriver => "Đang giao",
            DeliveryRunStatus.ReadyForHandover => "Chờ bàn giao",
            DeliveryRunStatus.Closed => "Đã đóng",
            DeliveryRunStatus.Planning => "Đang lập",
            _ => status.ToString()
        };

        return new DeliveryRunProgressInfo(statusName, deliveredCount, isCompleted);
    }

    private static decimal GetPendingCashAmount(IEnumerable<DeliveryRunItemModel> items)
        => items
            .Where(item => item.DeliveryNoteStatus != (int)DeliveryNoteStatus.Delivered &&
                           item.DeliveryNoteStatus != (int)DeliveryNoteStatus.Cancelled)
            .Sum(item => item.AmountToCollect);

    private sealed record DeliveryRunProgressInfo(
        string StatusName,
        int DeliveredItemCount,
        bool IsCompleted);
}

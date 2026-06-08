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
                pagedData.Items.Select(ToListModel).ToList(),
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

        return new DeliveryRunDetailsModel
        {
            Id = run.Id,
            Code = run.Code,
            AssignedDeliveryUserId = run.AssignedDeliveryUserId,
            AssignedDeliveryUsername = run.AssignedDeliveryUsername,
            AssignedDeliveryFullName = run.AssignedDeliveryFullName,
            Status = run.Status,
            StatusName = GetStatusName((DeliveryRunStatus)run.Status),
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
            Items = run.Items.Select(item =>
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
                    CustomerPhone = note?.CustomerPhone,
                    ShippingAddress = note?.ShippingAddress ?? item.ShippingAddress,
                    AmountToCollect = note?.AmountToCollect ?? item.AmountToCollect,
                    CashCollectedAmount = note?.DeliveryCashCollectedAmount,
                    ReceiverName = note?.DeliveryReceiverName,
                    DeliveryProofPictureId = note?.DeliveryProofPictureId,
                    ProductItems = note?.Items.Select(product => new DeliveryRunProductItemModel
                    {
                        ProductName = product.ProductName,
                        Quantity = product.Quantity
                    }).ToList() ?? []
                };
            }).ToList()
        };
    }

    public async Task<DeliveryMobileIndexModel> PrepareDeliveryMobileIndexModelAsync(Guid currentUserId, string currentUserFullName)
    {
        var pagedData = await deliveryRunAppService.GetListAsync(
            0,
            50,
            null,
            currentUserId,
            null).ConfigureAwait(false);

        var activeRuns = pagedData.Items
            .Where(run => run.Status != (int)DeliveryRunStatus.Closed && run.Status != (int)DeliveryRunStatus.Cancelled)
            .ToList();
        var runs = new List<DeliveryMobileRunListItemModel>();
        foreach (var run in activeRuns)
        {
            var details = await PrepareDeliveryRunDetailsModelAsync(run.Id).ConfigureAwait(false);
            runs.Add(ToMobileListModel(run, details.Items));
        }

        return new DeliveryMobileIndexModel
        {
            CurrentUserId = currentUserId,
            CurrentUserFullName = currentUserFullName,
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

    private static DeliveryRunListItemModel ToListModel(DeliveryRunListAppDto run)
        => new()
        {
            Id = run.Id,
            Code = run.Code,
            AssignedDeliveryUserId = run.AssignedDeliveryUserId,
            AssignedDeliveryFullName = run.AssignedDeliveryFullName,
            Status = run.Status,
            StatusName = GetStatusName((DeliveryRunStatus)run.Status),
            ItemCount = run.ItemCount,
            AmountToCollect = run.AmountToCollect,
            DriverCachedOnUtc = run.DriverCachedOnUtc,
            PaperManifestIssued = run.PaperManifestIssued,
            HandedOverOnUtc = run.HandedOverOnUtc,
            CashHandoverConfirmedOnUtc = run.CashHandoverConfirmedOnUtc,
            CashHandoverAmount = run.CashHandoverAmount,
            CreatedOnUtc = run.CreatedOnUtc
        };

    private static DeliveryMobileRunListItemModel ToMobileListModel(DeliveryRunListAppDto run, IList<DeliveryRunItemModel> items)
        => new()
        {
            Id = run.Id,
            Code = run.Code,
            Status = run.Status,
            StatusName = GetStatusName((DeliveryRunStatus)run.Status),
            ItemCount = run.ItemCount,
            AmountToCollect = GetPendingCashAmount(items),
            DriverCachedOnUtc = run.DriverCachedOnUtc,
            PaperManifestIssued = run.PaperManifestIssued,
            HandedOverOnUtc = run.HandedOverOnUtc,
            CreatedOnUtc = run.CreatedOnUtc,
            Items = items
        };

    private static decimal GetPendingCashAmount(IEnumerable<DeliveryRunItemModel> items)
        => items.Sum(item =>
            item.AmountToCollect > item.CashCollectedAmount.GetValueOrDefault()
                ? item.AmountToCollect - item.CashCollectedAmount.GetValueOrDefault()
                : 0);

    private static string GetStatusName(DeliveryRunStatus status)
        => status switch
        {
            DeliveryRunStatus.Planning => "Đang lập",
            DeliveryRunStatus.ReadyForHandover => "Chờ bàn giao",
            DeliveryRunStatus.HandedToDriver => "Đã bàn giao",
            DeliveryRunStatus.Closed => "Đã đóng",
            DeliveryRunStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };
}

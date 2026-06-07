using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Domain.Shared.Dtos.Users;
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
        model.AvailableDeliveryNotes = await PrepareCandidateDeliveryNotesAsync(model.AssignedDeliveryUserId).ConfigureAwait(false);
        return model;
    }

    public async Task<DeliveryRunDetailsModel> PrepareDeliveryRunDetailsModelAsync(Guid id)
    {
        var run = await deliveryRunAppService.GetByIdAsync(id).ConfigureAwait(false)
                  ?? throw new ArgumentException("Delivery run not found");

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
            Items = run.Items.Select(item => new DeliveryRunItemModel
            {
                Id = item.Id,
                DeliveryNoteId = item.DeliveryNoteId,
                DeliveryNoteCode = item.DeliveryNoteCode,
                OrderCode = item.OrderCode,
                CustomerName = item.CustomerName,
                ShippingAddress = item.ShippingAddress,
                AmountToCollect = item.AmountToCollect
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

        return new DeliveryMobileIndexModel
        {
            CurrentUserId = currentUserId,
            CurrentUserFullName = currentUserFullName,
            Runs = pagedData.Items
                .Where(run => run.Status != (int)DeliveryRunStatus.Closed && run.Status != (int)DeliveryRunStatus.Cancelled)
                .Select(ToListModel)
                .ToList()
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

    private async Task<IList<DeliveryRunCandidateDeliveryNoteModel>> PrepareCandidateDeliveryNotesAsync(Guid assignedDeliveryUserId)
    {
        if (assignedDeliveryUserId == Guid.Empty)
            return [];

        var notes = await deliveryNoteAppService.GetListAsync(null, 0, 500).ConfigureAwait(false);
        return notes.Items
            .Where(note => note.Status == (int)DeliveryNoteStatus.Confirmed)
            .Where(note => !note.IsDirectShip && note.SourceType == (int)DeliveryNoteSourceType.ToCustomer)
            .Where(note => note.AssignedDeliveryUserId == assignedDeliveryUserId)
            .OrderBy(note => note.CreatedOnUtc)
            .Select(note => new DeliveryRunCandidateDeliveryNoteModel
            {
                Id = note.Id,
                Code = note.Code,
                OrderCode = note.OrderCode,
                CustomerName = note.CustomerName,
                ShippingAddress = note.ShippingAddress,
                AmountToCollect = note.AmountToCollect,
                CreatedOnUtc = note.CreatedOnUtc
            })
            .ToList();
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

    private static string GetStatusName(DeliveryRunStatus status)
        => status switch
        {
            DeliveryRunStatus.Planning => "Dang lap",
            DeliveryRunStatus.ReadyForHandover => "Cho ban giao",
            DeliveryRunStatus.HandedToDriver => "Da ban giao",
            DeliveryRunStatus.Closed => "Da dong",
            DeliveryRunStatus.Cancelled => "Da huy",
            _ => status.ToString()
        };
}

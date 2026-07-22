using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Application.Services.Notifications;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;

namespace NamEcommerce.Application.Services.DeliveryNotes;

public sealed class DeliveryRunAppService(
    IDeliveryRunManager manager, IUserAppService userAppService,
    ISystemNotificationAppService notificationAppService) : IDeliveryRunAppService
{
    public async Task<CreateDeliveryRunResultAppDto> CreateAsync(CreateDeliveryRunAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new CreateDeliveryRunResultAppDto { Success = false, ErrorMessage = errorMessage };

        var user = await userAppService.GetUserByIdAsync(dto.AssignedDeliveryUserId).ConfigureAwait(false);
        if (user is null)
            return new CreateDeliveryRunResultAppDto { Success = false, ErrorMessage = "Error.UserNotFound" };

        var isDeliveryStaff = await userAppService
            .IsUserInRoleAsync(user.Id, SystemUserRoleNames.DeliveryStaff)
            .ConfigureAwait(false);
        if (!isDeliveryStaff)
            return new CreateDeliveryRunResultAppDto { Success = false, ErrorMessage = "Error.UserIsNotDeliveryStaff" };

        var run = await manager.CreateAsync(new CreateDeliveryRunDto
        {
            AssignedDeliveryUserId = user.Id,
            AssignedDeliveryUsername = user.Username,
            AssignedDeliveryFullName = user.FullName,
            DeliveryNoteIds = dto.DeliveryNoteIds,
            Note = dto.Note
        }).ConfigureAwait(false);

        await notificationAppService
            .CreateAsync(DeliverySystemNotificationComposer.DeliveryRunCreated(run.ToAppDto()))
            .ConfigureAwait(false);

        return new CreateDeliveryRunResultAppDto { Success = true, CreatedId = run.Id };
    }

    public async Task<CommonActionResultDto> AcknowledgeDriverCacheAsync(Guid id, string? deviceId)
    {
        await manager.AcknowledgeDriverCacheAsync(id, deviceId).ConfigureAwait(false);
        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> IssuePaperManifestAsync(Guid id)
    {
        await manager.IssuePaperManifestAsync(id).ConfigureAwait(false);
        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> ConfirmWarehousePickAsync(Guid id, Guid warehouseId)
    {
        await manager.ConfirmWarehousePickAsync(id, warehouseId).ConfigureAwait(false);
        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> HandOverAsync(Guid id)
    {
        var run = await manager.GetByIdAsync(id).ConfigureAwait(false);
        if (run is null)
            return CommonActionResultDto.CreateError("Error.DeliveryRunNotFound");

        await manager.HandOverAsync(id).ConfigureAwait(false);

        var appDto = run.ToAppDto();
        await notificationAppService.CreateAsync(DeliverySystemNotificationComposer.DeliveryRunHandedOver(appDto))
            .ConfigureAwait(false);

        if (appDto.Items.Sum(item => item.AmountToCollect) > 0 && appDto.CashHandoverConfirmedOnUtc is null)
        {
            await notificationAppService.CreateAsync(DeliverySystemNotificationComposer.DeliveryRunCashHandoverPending(appDto))
                .ConfigureAwait(false);
        }

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> CloseAsync(Guid id)
    {
        await manager.CloseAsync(id).ConfigureAwait(false);
        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> ConfirmCashHandoverAsync(ConfirmDeliveryRunCashHandoverAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return CommonActionResultDto.CreateError(errorMessage!);

        await manager.ConfirmCashHandoverAsync(new ConfirmDeliveryRunCashHandoverDto
        {
            DeliveryRunId = dto.DeliveryRunId,
            Amount = dto.Amount,
            Note = dto.Note
        }).ConfigureAwait(false);
        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> UpdateDeliveredNoteCashCollectedAsync(UpdateDeliveryRunDeliveredNoteCashCollectedAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return CommonActionResultDto.CreateError(errorMessage!);

        await manager.UpdateDeliveredNoteCashCollectedAsync(new UpdateDeliveryRunDeliveredNoteCashCollectedDto
        {
            DeliveryRunId = dto.DeliveryRunId,
            DeliveryNoteId = dto.DeliveryNoteId,
            CashCollectedAmount = dto.CashCollectedAmount
        }).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> CancelAsync(Guid id)
    {
        await manager.CancelAsync(id).ConfigureAwait(false);
        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<DeliveryRunAppDto?> GetByIdAsync(Guid id)
    {
        var run = await manager.GetByIdAsync(id).ConfigureAwait(false);
        return run?.ToAppDto();
    }

    public async Task<DeliveryRunAppDto?> GetByDeliveryNoteIdAsync(Guid deliveryNoteId)
    {
        var run = await manager.GetByDeliveryNoteIdAsync(deliveryNoteId).ConfigureAwait(false);
        return run?.ToAppDto();
    }

    public async Task<IPagedDataAppDto<DeliveryRunListAppDto>> GetListAsync(int pageIndex, int pageSize,
        string? keywords, Guid? assignedDeliveryUserId, int? status)
    {
        var typedStatus = status.HasValue ? (DeliveryRunStatus?)status.Value : null;
        var paged = await manager.GetListAsync(pageIndex, pageSize, keywords, assignedDeliveryUserId, typedStatus)
            .ConfigureAwait(false);

        return PagedDataAppDto.Create(
            paged.Items.Select(run => run.ToListAppDto()).ToList(),
            paged.PagerInfo.PageIndex,
            paged.PagerInfo.PageSize,
            paged.PagerInfo.TotalCount);
    }
}

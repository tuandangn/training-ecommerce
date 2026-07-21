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
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.DeliveryNotes;

public sealed class DeliveryRunAppService(
    IDeliveryRunManager manager,
    IUserAppService userAppService,
    ISystemNotificationAppService notificationAppService)
    : IDeliveryRunAppService
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

        try
        {
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
        catch (NamEcommerceDomainException ex)
        {
            return new CreateDeliveryRunResultAppDto { Success = false, ErrorMessage = ex.ErrorCode };
        }
    }

    public Task<CommonActionResultDto> AcknowledgeDriverCacheAsync(Guid id, string? deviceId)
        => RunActionAsync(() => manager.AcknowledgeDriverCacheAsync(id, deviceId));

    public Task<CommonActionResultDto> IssuePaperManifestAsync(Guid id)
        => RunActionAsync(() => manager.IssuePaperManifestAsync(id));

    public Task<CommonActionResultDto> ConfirmWarehousePickAsync(Guid id, Guid warehouseId)
        => RunActionAsync(() => manager.ConfirmWarehousePickAsync(id, warehouseId));

    public async Task<CommonActionResultDto> HandOverAsync(Guid id)
    {
        var result = await RunActionAsync(() => manager.HandOverAsync(id)).ConfigureAwait(false);
        if (!result.Success)
            return result;

        var run = await manager.GetByIdAsync(id).ConfigureAwait(false);
        if (run is null)
            return result;

        var appDto = run.ToAppDto();
        await notificationAppService
            .CreateAsync(DeliverySystemNotificationComposer.DeliveryRunHandedOver(appDto))
            .ConfigureAwait(false);

        if (appDto.Items.Sum(item => item.AmountToCollect) > 0 && appDto.CashHandoverConfirmedOnUtc is null)
        {
            await notificationAppService
                .CreateAsync(DeliverySystemNotificationComposer.DeliveryRunCashHandoverPending(appDto))
                .ConfigureAwait(false);
        }

        return result;
    }

    public Task<CommonActionResultDto> CloseAsync(Guid id)
        => RunActionAsync(() => manager.CloseAsync(id));

    public Task<CommonActionResultDto> ConfirmCashHandoverAsync(ConfirmDeliveryRunCashHandoverAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return Task.FromResult(CommonActionResultDto.CreateError(errorMessage!));

        return RunActionAsync(() => manager.ConfirmCashHandoverAsync(new ConfirmDeliveryRunCashHandoverDto
        {
            DeliveryRunId = dto.DeliveryRunId,
            Amount = dto.Amount,
            Note = dto.Note
        }));
    }

    public Task<CommonActionResultDto> UpdateDeliveredNoteCashCollectedAsync(UpdateDeliveryRunDeliveredNoteCashCollectedAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return Task.FromResult(CommonActionResultDto.CreateError(errorMessage!));

        return RunActionAsync(() => manager.UpdateDeliveredNoteCashCollectedAsync(new UpdateDeliveryRunDeliveredNoteCashCollectedDto
        {
            DeliveryRunId = dto.DeliveryRunId,
            DeliveryNoteId = dto.DeliveryNoteId,
            CashCollectedAmount = dto.CashCollectedAmount
        }));
    }

    public Task<CommonActionResultDto> CancelAsync(Guid id)
        => RunActionAsync(() => manager.CancelAsync(id));

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
        var paged = await manager
            .GetListAsync(pageIndex, pageSize, keywords, assignedDeliveryUserId, typedStatus)
            .ConfigureAwait(false);

        return PagedDataAppDto.Create(
            paged.Items.Select(run => run.ToListAppDto()).ToList(),
            paged.PagerInfo.PageIndex,
            paged.PagerInfo.PageSize,
            paged.PagerInfo.TotalCount);
    }

    private static async Task<CommonActionResultDto> RunActionAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
            return CommonActionResultDto.CreateSuccess();
        }
        catch (NamEcommerceDomainException ex)
        {
            return CommonActionResultDto.CreateError(ex.ErrorCode);
        }
    }
}

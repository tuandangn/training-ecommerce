using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Notifications;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Notifications;
using NamEcommerce.Domain.Shared.Enums.Notifications;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.Notifications;

namespace NamEcommerce.Application.Services.Notifications;

public sealed class SystemNotificationAppService(ISystemNotificationManager manager) : ISystemNotificationAppService
{
    public async Task<CreateSystemNotificationResultAppDto> CreateAsync(CreateSystemNotificationAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new CreateSystemNotificationResultAppDto { Success = false, ErrorMessage = errorMessage };

        if (!TryGetNotificationType(dto.Type, out var type))
            return new CreateSystemNotificationResultAppDto { Success = false, ErrorMessage = "Error.SystemNotificationTypeInvalid" };
        if (!TryGetNotificationSeverity(dto.Severity, out var severity))
            return new CreateSystemNotificationResultAppDto { Success = false, ErrorMessage = "Error.SystemNotificationSeverityInvalid" };

        try
        {
            var notification = await manager.CreateAsync(new CreateSystemNotificationDto
            {
                Type = type,
                Severity = severity,
                Title = dto.Title,
                Message = dto.Message,
                RequiredPermission = dto.RequiredPermission,
                RelatedEntityType = dto.RelatedEntityType,
                RelatedEntityId = dto.RelatedEntityId,
                ActionUrl = dto.ActionUrl,
                CreatedByUserId = dto.CreatedByUserId
            }).ConfigureAwait(false);

            return new CreateSystemNotificationResultAppDto
            {
                Success = true,
                CreatedId = notification.Id,
                Notification = notification.ToAppDto()
            };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new CreateSystemNotificationResultAppDto { Success = false, ErrorMessage = ex.ErrorCode };
        }
        catch (ArgumentException ex)
        {
            return new CreateSystemNotificationResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<IPagedDataAppDto<SystemNotificationAppDto>> GetNotificationsAsync(SystemNotificationListFilterAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            throw new ArgumentException(errorMessage);

        var domainFilter = new SystemNotificationListFilterDto
        {
            UserPermissions = dto.UserPermissions,
            UserId = dto.UserId,
            Type = GetNullableNotificationType(dto.Type),
            Types = dto.Types?.Select(t => (SystemNotificationType)t).Where(t => Enum.IsDefined(t)).ToList(),
            Severity = GetNullableNotificationSeverity(dto.Severity),
            IsRead = dto.IsRead,
            PageIndex = dto.PageIndex,
            PageSize = dto.PageSize
        };

        var paged = await manager.GetNotificationsAsync(domainFilter).ConfigureAwait(false);
        return PagedDataAppDto.Create(
            paged.Items.Select(item => item.ToAppDto()).ToList(),
            paged.PagerInfo.PageIndex,
            paged.PagerInfo.PageSize,
            paged.PagerInfo.TotalCount);
    }

    public Task<int> CountUnreadAsync(Guid userId, IReadOnlyCollection<string> userPermissions)
        => manager.CountUnreadAsync(userId, userPermissions);

    public Task<CommonActionResultDto> MarkReadAsync(Guid notificationId, Guid userId)
        => RunActionAsync(() => manager.MarkReadAsync(notificationId, userId));

    public Task<CommonActionResultDto> MarkAllReadAsync(Guid userId, IReadOnlyCollection<string> userPermissions)
        => RunActionAsync(() => manager.MarkAllReadAsync(userId, userPermissions));

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
        catch (ArgumentException ex)
        {
            return CommonActionResultDto.CreateError(ex.Message);
        }
    }

    private static SystemNotificationType? GetNullableNotificationType(int? value)
    {
        if (!value.HasValue)
            return null;
        if (!TryGetNotificationType(value.Value, out var type))
            throw new ArgumentException("Error.SystemNotificationTypeInvalid");

        return type;
    }

    private static SystemNotificationSeverity? GetNullableNotificationSeverity(int? value)
    {
        if (!value.HasValue)
            return null;
        if (!TryGetNotificationSeverity(value.Value, out var severity))
            throw new ArgumentException("Error.SystemNotificationSeverityInvalid");

        return severity;
    }

    private static bool TryGetNotificationType(int value, out SystemNotificationType type)
    {
        type = (SystemNotificationType)value;
        return Enum.IsDefined(type);
    }

    private static bool TryGetNotificationSeverity(int value, out SystemNotificationSeverity severity)
    {
        severity = (SystemNotificationSeverity)value;
        return Enum.IsDefined(severity);
    }
}

using NamEcommerce.Application.Contracts.Dtos.Notifications;
using NamEcommerce.Domain.Shared.Dtos.Notifications;

namespace NamEcommerce.Application.Services.Extensions;

public static class SystemNotificationExtensions
{
    public static SystemNotificationAppDto ToAppDto(this SystemNotificationDto dto) =>
        new()
        {
            Id = dto.Id,
            Type = (int)dto.Type,
            Severity = (int)dto.Severity,
            Title = dto.Title,
            Message = dto.Message,
            RequiredPermission = dto.RequiredPermission,
            RelatedEntityType = dto.RelatedEntityType,
            RelatedEntityId = dto.RelatedEntityId,
            ActionUrl = dto.ActionUrl,
            CreatedByUserId = dto.CreatedByUserId,
            CreatedOnUtc = dto.CreatedOnUtc,
            ReadOnUtc = dto.ReadOnUtc
        };
}

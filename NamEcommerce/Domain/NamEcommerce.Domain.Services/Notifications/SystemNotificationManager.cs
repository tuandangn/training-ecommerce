using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Notifications;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Notifications;
using NamEcommerce.Domain.Shared.Services.Notifications;

namespace NamEcommerce.Domain.Services.Notifications;

public sealed class SystemNotificationManager(
    IRepository<SystemNotification> notificationRepository,
    IEntityDataReader<SystemNotification> notificationReader,
    IRepository<SystemNotificationRead> notificationReadRepository,
    IEntityDataReader<SystemNotificationRead> notificationReadReader) : ISystemNotificationManager
{
    public async Task<SystemNotificationDto> CreateAsync(CreateSystemNotificationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var notification = new SystemNotification(
            dto.Type,
            dto.Severity,
            dto.Title,
            dto.Message,
            dto.RequiredPermission,
            dto.RelatedEntityType,
            dto.RelatedEntityId,
            dto.ActionUrl,
            dto.CreatedByUserId);

        var inserted = await notificationRepository.InsertAsync(notification).ConfigureAwait(false);
        return ToDto(inserted, null);
    }

    public Task<IPagedDataDto<SystemNotificationDto>> GetNotificationsAsync(SystemNotificationListFilterDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentOutOfRangeException.ThrowIfLessThan(dto.PageIndex, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(dto.PageSize, 0);

        var permissions = BuildPermissionSet(dto.UserPermissions);
        var query = notificationReader.DataSource
            .Where(notification => permissions.Contains(notification.RequiredPermission));

        if (dto.Types is { Count: > 0 })
            query = query.Where(notification => dto.Types.Contains(notification.Type));
        else if (dto.Type.HasValue)
            query = query.Where(notification => notification.Type == dto.Type.Value);
        if (dto.Severity.HasValue)
            query = query.Where(notification => notification.Severity == dto.Severity.Value);

        var readByNotificationId = GetReadMap(dto.UserId);
        if (dto.IsRead.HasValue && dto.UserId.HasValue)
        {
            query = dto.IsRead.Value
                ? query.Where(notification => readByNotificationId.Keys.Contains(notification.Id))
                : query.Where(notification => !readByNotificationId.Keys.Contains(notification.Id));
        }

        var total = query.Count();
        var items = query
            .OrderByDescending(notification => notification.CreatedOnUtc)
            .Skip(dto.PageIndex * dto.PageSize)
            .Take(dto.PageSize)
            .ToList()
            .Select(notification => ToDto(notification, readByNotificationId.GetValueOrDefault(notification.Id)))
            .ToList();

        return Task.FromResult(PagedDataDto.Create(items, dto.PageIndex, dto.PageSize, total));
    }

    public Task<int> CountUnreadAsync(Guid userId, IReadOnlyCollection<string> userPermissions)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var permissions = BuildPermissionSet(userPermissions);
        var readNotificationIds = notificationReadReader.DataSource
            .Where(read => read.UserId == userId)
            .Select(read => read.NotificationId)
            .ToHashSet();

        var count = notificationReader.DataSource.Count(notification =>
            permissions.Contains(notification.RequiredPermission) &&
            !readNotificationIds.Contains(notification.Id));

        return Task.FromResult(count);
    }

    public async Task MarkReadAsync(Guid notificationId, Guid userId)
    {
        if (notificationId == Guid.Empty)
            throw new ArgumentException("NotificationId is required.", nameof(notificationId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var notification = await notificationReader.GetByIdAsync(notificationId, default).ConfigureAwait(false);
        if (notification is null)
            throw new ArgumentException("Notification is not found.", nameof(notificationId));

        var exists = notificationReadReader.DataSource.Any(read =>
            read.NotificationId == notificationId && read.UserId == userId);
        if (exists)
            return;

        await notificationReadRepository.InsertAsync(new SystemNotificationRead(notificationId, userId, DateTime.UtcNow))
            .ConfigureAwait(false);
    }

    public async Task MarkAllReadAsync(Guid userId, IReadOnlyCollection<string> userPermissions)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        var permissions = BuildPermissionSet(userPermissions);
        var existingReadIds = notificationReadReader.DataSource
            .Where(read => read.UserId == userId)
            .Select(read => read.NotificationId)
            .ToHashSet();

        var unreadIds = notificationReader.DataSource
            .Where(notification => permissions.Contains(notification.RequiredPermission))
            .Select(notification => notification.Id)
            .Where(id => !existingReadIds.Contains(id))
            .ToList();

        foreach (var notificationId in unreadIds)
        {
            await notificationReadRepository.InsertAsync(new SystemNotificationRead(notificationId, userId, DateTime.UtcNow))
                .ConfigureAwait(false);
        }
    }

    private Dictionary<Guid, DateTime> GetReadMap(Guid? userId)
    {
        if (!userId.HasValue)
            return new Dictionary<Guid, DateTime>();

        return notificationReadReader.DataSource
            .Where(read => read.UserId == userId.Value)
            .GroupBy(read => read.NotificationId)
            .ToDictionary(group => group.Key, group => group.Max(read => read.ReadOnUtc));
    }

    private static HashSet<string> BuildPermissionSet(IEnumerable<string> permissions)
        => permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static SystemNotificationDto ToDto(SystemNotification notification, DateTime? readOnUtc)
        => new(notification.Id)
        {
            Type = notification.Type,
            Severity = notification.Severity,
            Title = notification.Title,
            Message = notification.Message,
            RequiredPermission = notification.RequiredPermission,
            RelatedEntityType = notification.RelatedEntityType,
            RelatedEntityId = notification.RelatedEntityId,
            ActionUrl = notification.ActionUrl,
            CreatedByUserId = notification.CreatedByUserId,
            CreatedOnUtc = notification.CreatedOnUtc,
            ReadOnUtc = readOnUtc
        };
}

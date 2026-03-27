using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Events.Entities;

namespace NamEcommerce.Domain.Services.Extensions;

public static class EventPublisherExtensions
{
    public static Task EntityCreated<TEntity>(this IEventPublisher publisher, TEntity entity) where TEntity : AppEntity
        => publisher.PublishEvent<EntityCreatedEvent<TEntity>, TEntity>(new EntityCreatedEvent<TEntity>(entity));

    public static Task EntityUpdated<TEntity>(this IEventPublisher publisher, TEntity entity, object? additionalData = null) where TEntity : AppEntity
        => publisher.PublishEvent<EntityUpdatedEvent<TEntity>, TEntity>(new EntityUpdatedEvent<TEntity>(entity, additionalData));

    public static Task EntityDeleted<TEntity>(this IEventPublisher publisher, TEntity entity) where TEntity : AppEntity
        => publisher.PublishEvent<EntityDeletedEvent<TEntity>, TEntity>(new EntityDeletedEvent<TEntity>(entity));
}

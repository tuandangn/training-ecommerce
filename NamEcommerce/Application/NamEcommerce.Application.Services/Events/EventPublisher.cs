using MediatR;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Events.Entities;

namespace NamEcommerce.Application.Services.Events;

public sealed class EventPublisher : IEventPublisher
{
    private readonly IMediator _mediator;

    public EventPublisher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task PublishEvent<TEvent, TEntity>(TEvent @event)
        where TEvent : BaseEvent<TEntity>
        where TEntity : AppEntity
    {
        object publishedNotification = @event switch
        {
            EntityCreatedEvent<TEntity> => new EntityCreatedNotification<TEntity>(@event.Entity),
            EntityDeletedEvent<TEntity> => new EntityDeletedNotification<TEntity>(@event.Entity),
            EntityUpdatedEvent<TEntity> updatedEvent => new EntityUpdatedNotification<TEntity>(@event.Entity)
            {
                AdditionalData = updatedEvent.AdditionalData
            },
            _ => throw new NotSupportedException()
        };
        return _mediator.Publish(publishedNotification);
    }
}

[Serializable]
public sealed record EntityCreatedNotification<TEntity>(TEntity Entity) : INotification where TEntity : AppEntity;

[Serializable]
public sealed record EntityUpdatedNotification<TEntity>(TEntity Entity) : INotification where TEntity : AppEntity
{
    public object? AdditionalData { get; set; }
}

[Serializable]
public sealed record EntityDeletedNotification<TEntity>(TEntity Entity) : INotification where TEntity : AppEntity;

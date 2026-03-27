namespace NamEcommerce.Domain.Shared.Events;

public interface IEventPublisher
{
    Task PublishEvent<TEvent, TEntity>(TEvent @event) 
        where TEvent : BaseEvent<TEntity> 
        where TEntity : AppEntity;
}

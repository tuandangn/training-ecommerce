namespace NamEcommerce.Domain.Shared.Events;

[Serializable]
public class BaseEvent
{
}

[Serializable]
public class BaseEvent<TEntity> : BaseEvent where TEntity : AppEntity
{
    public BaseEvent(TEntity entity)
    {
        Entity = entity;
    }

    public TEntity Entity { get; set; }
}

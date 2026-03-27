namespace NamEcommerce.Domain.Shared.Events;

[Serializable]
public class BaseEvent<TEntity> where TEntity : AppEntity
{
    public BaseEvent(TEntity entity)
    {
        Entity = entity;
    }

    public TEntity Entity { get; set; }
}

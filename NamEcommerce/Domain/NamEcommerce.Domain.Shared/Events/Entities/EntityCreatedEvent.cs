namespace NamEcommerce.Domain.Shared.Events.Entities;

[Serializable]
public sealed class EntityCreatedEvent<TEntity> : BaseEvent<TEntity> where TEntity : AppEntity
{
    public EntityCreatedEvent(TEntity entity) : base(entity)
    {
    }
}

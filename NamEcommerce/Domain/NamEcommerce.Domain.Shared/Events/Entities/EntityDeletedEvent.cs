namespace NamEcommerce.Domain.Shared.Events.Entities;

[Serializable]
public sealed class EntityDeletedEvent<TEntity> : BaseEvent<TEntity> where TEntity : AppEntity
{
    public EntityDeletedEvent(TEntity entity) : base(entity)
    {
    }
}

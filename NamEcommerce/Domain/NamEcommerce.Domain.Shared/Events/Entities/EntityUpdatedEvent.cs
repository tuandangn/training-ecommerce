namespace NamEcommerce.Domain.Shared.Events.Entities;

[Serializable]
public sealed class EntityUpdatedEvent<TEntity> : BaseEvent<TEntity> where TEntity : AppEntity
{
    public EntityUpdatedEvent(TEntity entity, object? additionalData = null) : base(entity)
        => AdditionalData = additionalData; 

    public object? AdditionalData { get; set; }
}

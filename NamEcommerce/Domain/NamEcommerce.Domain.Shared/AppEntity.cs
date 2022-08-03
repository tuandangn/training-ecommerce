namespace NamEcommerce.Domain.Shared;

[Serializable]
public abstract record AppEntity
{
    public AppEntity(Guid id)
        => Id = id;

    public Guid Id { get; private set; }
}

namespace NamEcommerce.Domain.Shared;

[Serializable]
public abstract record AppEntity
{
    public AppEntity(int id)
        => Id = id;

    public int Id { get; private set; }
}

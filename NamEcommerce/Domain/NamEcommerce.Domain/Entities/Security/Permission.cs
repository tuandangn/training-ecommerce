namespace NamEcommerce.Domain.Entities.Security;

[Serializable]
public sealed record Permission : AppEntity
{
    public Permission(int id, string name) : base(id)
        => Name = name;

    public string Name { get; init; }
}

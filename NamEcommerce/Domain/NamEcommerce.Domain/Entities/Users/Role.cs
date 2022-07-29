namespace NamEcommerce.Domain.Entities.Users;

[Serializable]
public sealed record Role : AppEntity
{
    public Role(int id, string name) : base(id)
        => Name = name;

    public string Name { get; init; }
}

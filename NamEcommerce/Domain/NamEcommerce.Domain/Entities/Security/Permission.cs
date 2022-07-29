using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Security;

[Serializable]
public sealed record Permission : AppAggregateEntity
{
    public Permission(int id, string name) : base(id)
        => Name = name;

    public string Name { get; init; }
}

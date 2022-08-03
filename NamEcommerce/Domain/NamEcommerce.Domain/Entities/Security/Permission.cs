using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Security;

[Serializable]
public sealed record Permission : AppAggregateEntity
{
    public Permission(Guid id, string name) : base(id)
        => Name = name;

    public string Name { get; init; }
}

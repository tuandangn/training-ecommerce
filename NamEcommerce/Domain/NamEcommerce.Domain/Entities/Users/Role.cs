using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Users;

[Serializable]
public sealed record Role : AppAggregateEntity
{
    public Role(Guid id, string name) : base(id)
        => Name = name;

    public string Name { get; init; }
}

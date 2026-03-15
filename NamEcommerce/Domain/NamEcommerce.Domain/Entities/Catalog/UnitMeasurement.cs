using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record UnitMeasurement : AppAggregateEntity
{
    internal UnitMeasurement(Guid id, string name) : base(id)
        => Name = name;

    public string Name { get; init; }
    public int DisplayOrder { get; set; }
}

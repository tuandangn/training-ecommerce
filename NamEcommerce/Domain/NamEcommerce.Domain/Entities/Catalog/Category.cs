using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record Category : AppAggregateEntity
{
    internal Category(int id, string name) : base(id)
        => Name = name;

    public string Name { get; init; }
    public int DisplayOrder { get; set; }

    public int? ParentId { get; internal set; }
    public int OnParentDisplayOrder { get; internal set; }

    public DateTime CreatedOnUtc { get; init; }
}

using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record Category : AppAggregateEntity
{
    internal Category(Guid id, string name) : base(id)
        => Name = name;

    public string Name { get; init; }
    public int DisplayOrder { get; set; }

    public Guid? ParentId { get; internal set; }
    public int OnParentDisplayOrder { get; internal set; }

    public DateTime CreatedOnUtc { get; init; } 
        = DateTime.UtcNow;
}

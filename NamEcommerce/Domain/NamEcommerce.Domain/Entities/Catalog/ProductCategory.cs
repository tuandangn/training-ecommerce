using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record ProductCategory : AppEntity
{
    internal ProductCategory(Guid id, Guid productId, Guid categoryId, int displayOrder) : base(id)
        => (ProductId, CategoryId, DisplayOrder) = (productId, categoryId, displayOrder);

    public Guid ProductId { get; init; }
    public Guid CategoryId { get; init; }

    public int DisplayOrder { get; set; }
}

using System;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record ProductCategory : AppEntity
{
    internal ProductCategory(int id, int productId, int categoryId) : base(id)
        => (ProductId, CategoryId) = (productId, categoryId);

    public int ProductId { get; init; }

    public int CategoryId { get; init; }

    public int DisplayOrder { get; set; }
}

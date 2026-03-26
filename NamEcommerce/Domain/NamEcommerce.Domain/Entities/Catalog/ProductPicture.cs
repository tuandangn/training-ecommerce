using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record ProductPicture : AppEntity
{
    public ProductPicture(Guid id, Guid productId, Guid pictureId) : base(id)
        => (ProductId, PictureId) = (productId, pictureId);

    public Guid ProductId { get; init; }
    public Guid PictureId { get; init; }

    public int DisplayOrder { get; set; }
}

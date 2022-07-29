using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Media;

[Serializable]
public sealed record ProductPicture : AppEntity
{
    public ProductPicture(int id, int productId, int pictureId) : base(id)
        => (ProductId, PictureId) = (productId, pictureId);

    public int ProductId { get; init; }
    public int PictureId { get; init; }

    public int DisplayOrder { get; set; }
}

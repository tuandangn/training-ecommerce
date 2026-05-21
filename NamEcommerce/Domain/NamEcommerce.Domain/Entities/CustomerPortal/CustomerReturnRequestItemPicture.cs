using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerReturnRequestItemPicture : AppAggregateEntity
{
    private CustomerReturnRequestItemPicture() : base(Guid.NewGuid()) { }

    internal CustomerReturnRequestItemPicture(Guid customerReturnRequestItemId, Guid pictureId) : base(Guid.NewGuid())
    {
        if (pictureId == Guid.Empty)
            throw new ArgumentException("Picture is required.", nameof(pictureId));

        CustomerReturnRequestItemId = customerReturnRequestItemId;
        PictureId = pictureId;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid CustomerReturnRequestItemId { get; private set; }
    public Guid PictureId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
}

using MediatR;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services.Media;

namespace NamEcommerce.Application.Services.Events.Catalog;

public sealed class ProductDeletedEventHandler : INotificationHandler<EntityDeletedNotification<Product>>
{
    private readonly IPictureManager _pictureManager;

    public ProductDeletedEventHandler(IPictureManager pictureManager)
    {
        _pictureManager = pictureManager;
    }

    public async Task Handle(EntityDeletedNotification<Product> notification, CancellationToken cancellationToken)
    {
        var product = notification.Entity;
        if (!product.ProductPictures.Any())
            return;

        foreach (var productPicture in product.ProductPictures)
        {
            var picture = await _pictureManager.GetPictureByIdAsync(productPicture.PictureId).ConfigureAwait(false);
            if (picture is null)
                continue;

            await _pictureManager.DeletePictureAsync(productPicture.PictureId).ConfigureAwait(false);
        }
    }
}

using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Application.Services.Events.Catalog;

public sealed class ProductDeletedEventHandler : INotificationHandler<EntityDeletedNotification<Product>>
{
    private readonly IRepository<Picture> _pictureRepository;
    private readonly IEntityDataReader<Picture> _pictureDataReader;

    public ProductDeletedEventHandler(IRepository<Picture> pictureRepository, IEntityDataReader<Picture> pictureDataReader)
    {
        _pictureRepository = pictureRepository;
        _pictureDataReader = pictureDataReader;
    }

    public async Task Handle(EntityDeletedNotification<Product> notification, CancellationToken cancellationToken)
    {
        var product = notification.Entity;
        if (!product.ProductPictures.Any())
            return;

        foreach (var productPicture in product.ProductPictures)
        {
            var picture = await _pictureDataReader.GetByIdAsync(productPicture.PictureId).ConfigureAwait(false);
            if (picture is null)
                continue;

            await _pictureRepository.DeleteAsync(picture).ConfigureAwait(false);
        }
    }
}

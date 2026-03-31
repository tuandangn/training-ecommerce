using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Application.Services.Events.Catalog;

public sealed class ProductUpdatedEventHandler : INotificationHandler<EntityUpdatedNotification<Product>>
{
    private readonly IRepository<Picture> _pictureRepository;
    private readonly IEntityDataReader<Picture> _pictureDataReader;

    public ProductUpdatedEventHandler(IRepository<Picture> pictureRepository, IEntityDataReader<Picture> pictureDataReader)
    {
        _pictureRepository = pictureRepository;
        _pictureDataReader = pictureDataReader;
    }

    public async Task Handle(EntityUpdatedNotification<Product> notification, CancellationToken cancellationToken)
    {
        if (notification.AdditionalData is null || !typeof(IEnumerable<Guid>).IsAssignableFrom(notification.AdditionalData.GetType()))
            return;

        var deletedPictureIds = (IEnumerable<Guid>)notification.AdditionalData;
        if (!deletedPictureIds.Any())
            return;

        foreach (var pictureId in deletedPictureIds)
        {
            var picture = await _pictureDataReader.GetByIdAsync(pictureId).ConfigureAwait(false);
            if (picture is null)
                continue;

            await _pictureRepository.DeleteAsync(picture).ConfigureAwait(false);
        }
    }
}
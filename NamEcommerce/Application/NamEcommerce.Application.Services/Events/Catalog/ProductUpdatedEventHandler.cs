using MediatR;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services.Media;

namespace NamEcommerce.Application.Services.Events.Catalog;

public sealed class ProductUpdatedEventHandler : INotificationHandler<EntityUpdatedNotification<Product>>
{
    private readonly IPictureManager _pictureManager;

    public ProductUpdatedEventHandler(IPictureManager pictureManager)
    {
        _pictureManager = pictureManager;
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
            var picture = await _pictureManager.GetPictureByIdAsync(pictureId).ConfigureAwait(false);
            if (picture is null)
                continue;

            await _pictureManager.DeletePictureAsync(picture.Id).ConfigureAwait(false);
        }
    }
}
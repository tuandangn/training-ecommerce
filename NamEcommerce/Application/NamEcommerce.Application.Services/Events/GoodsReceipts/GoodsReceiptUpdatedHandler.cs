using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Application.Services.Events.GoodsReceipts;

public sealed class GoodsReceiptUpdatedHandler(IRepository<Picture> pictureRepository, IEntityDataReader<Picture> pictureDataReader) 
    : INotificationHandler<EntityUpdatedNotification<GoodsReceipt>>
{
    public async Task Handle(EntityUpdatedNotification<GoodsReceipt> notification, CancellationToken cancellationToken)
    {
        if (notification.AdditionalData is null || !typeof(IEnumerable<Guid>).IsAssignableFrom(notification.AdditionalData.GetType()))
            return;

        var deletedPictureIds = (IEnumerable<Guid>)notification.AdditionalData;
        if (!deletedPictureIds.Any())
            return;

        foreach (var pictureId in deletedPictureIds)
        {
            var picture = await pictureDataReader.GetByIdAsync(pictureId).ConfigureAwait(false);
            if (picture is null)
                continue;

            await pictureRepository.DeleteAsync(picture).ConfigureAwait(false);
        }
    }
}

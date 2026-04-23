using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Application.Services.Events.GoodsReceipts;

public sealed class GoodsReceiptDeletedEventHandler(IRepository<Picture> pictureRepository, IEntityDataReader<Picture> pictureDataReader)
    : INotificationHandler<EntityDeletedNotification<GoodsReceipt>>
{
    public async Task Handle(EntityDeletedNotification<GoodsReceipt> notification, CancellationToken cancellationToken)
    {
        var product = notification.Entity;
        if (!product.PictureIds.Any())
            return;

        foreach (var pictureId in product.PictureIds)
        {
            var picture = await pictureDataReader.GetByIdAsync(pictureId).ConfigureAwait(false);
            if (picture is null)
                continue;

            await pictureRepository.DeleteAsync(picture).ConfigureAwait(false);
        }
    }
}

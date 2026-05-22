using MediatR;
using NamEcommerce.Domain.Shared.Events.Catalog;
using NamEcommerce.Domain.Shared.Services.Media;

namespace NamEcommerce.Application.Services.Events.Catalog;

/// <summary>
/// Sau khi sản phẩm bị xoá: dọn toàn bộ <see cref="Picture"/> đính kèm sản phẩm khỏi storage.
/// </summary>
public sealed class ProductDeletedEventHandler : INotificationHandler<ProductDeleted>
{
    private readonly IPictureManager _pictureManager;

    public ProductDeletedEventHandler(IPictureManager pictureManager)
    {
        _pictureManager = pictureManager;
    }

    public async Task Handle(ProductDeleted notification, CancellationToken cancellationToken)
    {
        if (notification.PictureIds is null || notification.PictureIds.Count == 0)
            return;

        foreach (var pictureId in notification.PictureIds)
        {
            var picture = await _pictureManager.GetPictureByIdAsync(pictureId).ConfigureAwait(false);
            if (picture is null)
                continue;

            await _pictureManager.DeletePictureAsync(pictureId).ConfigureAwait(false);
        }
    }
}

using MediatR;
using NamEcommerce.Domain.Shared.Events.Catalog;
using NamEcommerce.Domain.Shared.Services.Media;

namespace NamEcommerce.Application.Services.Events.Catalog;

/// <summary>
/// Sau khi sản phẩm được cập nhật: dọn dẹp các <see cref="Picture"/> không còn liên kết với product.
/// </summary>
public sealed class ProductUpdatedEventHandler : INotificationHandler<ProductUpdated>
{
    private readonly IPictureManager _pictureManager;

    public ProductUpdatedEventHandler(IPictureManager pictureManager)
    {
        _pictureManager = pictureManager;
    }

    public async Task Handle(ProductUpdated notification, CancellationToken cancellationToken)
    {
        if (notification.DeletedPictureIds is null || notification.DeletedPictureIds.Count == 0)
            return;

        foreach (var pictureId in notification.DeletedPictureIds)
        {
            var picture = await _pictureManager.GetPictureByIdAsync(pictureId).ConfigureAwait(false);
            if (picture is null)
                continue;

            await _pictureManager.DeletePictureAsync(pictureId).ConfigureAwait(false);
        }
    }
}

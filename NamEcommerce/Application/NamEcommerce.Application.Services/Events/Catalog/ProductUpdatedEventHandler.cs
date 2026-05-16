using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Events.Catalog;

namespace NamEcommerce.Application.Services.Events.Catalog;

/// <summary>
/// Sau khi sản phẩm được cập nhật: dọn dẹp các <see cref="Picture"/> không còn liên kết với product.
/// </summary>
public sealed class ProductUpdatedEventHandler : INotificationHandler<ProductUpdated>
{
    private readonly IRepository<Picture> _pictureRepository;
    private readonly IEntityDataReader<Picture> _pictureDataReader;

    public ProductUpdatedEventHandler(IRepository<Picture> pictureRepository, IEntityDataReader<Picture> pictureDataReader)
    {
        _pictureRepository = pictureRepository;
        _pictureDataReader = pictureDataReader;
    }

    public async Task Handle(ProductUpdated notification, CancellationToken cancellationToken)
    {
        if (notification.DeletedPictureIds is null || notification.DeletedPictureIds.Count == 0)
            return;

        foreach (var pictureId in notification.DeletedPictureIds)
        {
            var picture = await _pictureDataReader.GetByIdAsync(pictureId).ConfigureAwait(false);
            if (picture is null)
                continue;

            await _pictureRepository.DeleteAsync(picture).ConfigureAwait(false);
        }
    }
}

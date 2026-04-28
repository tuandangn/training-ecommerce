using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Events.Catalog;

namespace NamEcommerce.Application.Services.Events.Catalog;

/// <summary>
/// Sau khi sản phẩm bị xoá: dọn toàn bộ <see cref="Picture"/> đính kèm sản phẩm khỏi storage.
/// </summary>
public sealed class ProductDeletedEventHandler : INotificationHandler<ProductDeleted>
{
    private readonly IRepository<Picture> _pictureRepository;
    private readonly IEntityDataReader<Picture> _pictureDataReader;

    public ProductDeletedEventHandler(IRepository<Picture> pictureRepository, IEntityDataReader<Picture> pictureDataReader)
    {
        _pictureRepository = pictureRepository;
        _pictureDataReader = pictureDataReader;
    }

    public async Task Handle(ProductDeleted notification, CancellationToken cancellationToken)
    {
        if (notification.PictureIds is null || notification.PictureIds.Count == 0)
            return;

        foreach (var pictureId in notification.PictureIds)
        {
            var picture = await _pictureDataReader.GetByIdAsync(pictureId).ConfigureAwait(false);
            if (picture is null)
                continue;

            await _pictureRepository.DeleteAsync(picture).ConfigureAwait(false);
        }
    }
}

using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Services.Media;

namespace NamEcommerce.Domain.Services.Media;

public sealed class PictureManager : IPictureManager
{
    private readonly IRepository<Picture> _pictureRepository;
    private readonly IEntityDataReader<Picture> _pictureDataReader;
    private readonly IEventPublisher _eventPublisher;

    public PictureManager(IRepository<Picture> pictureRepository, IEntityDataReader<Picture> pictureEntityDataReader, IEventPublisher eventPublisher)
    {
        _pictureRepository = pictureRepository;
        _pictureDataReader = pictureEntityDataReader;
        _eventPublisher = eventPublisher;
    }

    public async Task<CreatePictureResultDto> CreatePictureAsync(CreatePictureDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var insertedPicture = await _pictureRepository.InsertAsync(new Picture(dto.Data, dto.MimeType)
        {
            FileName = dto.FileName,
            Extension = dto.Extension
        }).ConfigureAwait(false);

        await _eventPublisher.EntityCreated(insertedPicture).ConfigureAwait(false);

        return new CreatePictureResultDto
        {
            CreatedId = insertedPicture.Id
        };
    }

    public async Task DeletePictureAsync(Guid id)
    {
        var picture = await _pictureDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (picture is null)
            throw new ArgumentException("Picture is not found", nameof(id));

        await _pictureRepository.DeleteAsync(picture).ConfigureAwait(false);

        await _eventPublisher.EntityDeleted(picture).ConfigureAwait(false);
    }

    public async Task<PictureDto?> GetPictureByIdAsync(Guid id)
    {
        var picture = await _pictureDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (picture is null)
            return null;

        return picture.ToDto();
    }
}

using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Services.Media;

namespace NamEcommerce.Domain.Services.Media;

public sealed class PictureManager : IPictureManager
{
    private readonly IRepository<Picture> _pictureRepository;
    private readonly IEntityDataReader<Picture> _pictureDataReader;

    public PictureManager(IRepository<Picture> pictureRepository, IEntityDataReader<Picture> pictureEntityDataReader)
    {
        _pictureRepository = pictureRepository;
        _pictureDataReader = pictureEntityDataReader;
    }

    public async Task<CreatePictureResultDto> CreatePictureAsync(CreatePictureDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var picture = new Picture(dto.Data, dto.MimeType)
        {
            FileName = dto.FileName,
            Extension = dto.Extension
        };
        picture.MarkCreated();

        var insertedPicture = await _pictureRepository.InsertAsync(picture).ConfigureAwait(false);

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

        picture.MarkDeleted();

        await _pictureRepository.DeleteAsync(picture).ConfigureAwait(false);
    }

    public async Task<PictureDto?> GetPictureByIdAsync(Guid id)
    {
        var picture = await _pictureDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (picture is null)
            return null;

        return picture.ToDto();
    }
}

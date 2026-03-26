using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Services;
using NamEcommerce.Domain.Shared.Services.Media;

namespace NamEcommerce.Domain.Services.Catalog;

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

        var insertedPicture = await _pictureRepository.InsertAsync(new Picture(Guid.NewGuid(), dto.Data, dto.MimeType)
        {
            FileName = dto.FileName,
            Extension = dto.Extension
        }).ConfigureAwait(false);

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
    }

    public async Task<PictureDto?> GetPictureByIdAsync(Guid id)
    {
        var picture = await _pictureDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (picture is null)
            return null;

        return picture.ToDto();
    }

    public async Task<UpdatePictureResultDto> UpdatePictureAsync(UpdatePictureDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var picture = await _pictureDataReader.GetByIdAsync(dto.Id);
        if (picture is null)
            throw new ArgumentException("Picture  is not found", nameof(dto));

        picture.Data = dto.Data;
        picture.MimeType = dto.MimeType;
        picture.FileName = dto.FileName;
        picture.Extension = dto.Extension;

        var result = await _pictureRepository.UpdateAsync(picture).ConfigureAwait(false);

        return new UpdatePictureResultDto(result.Id)
        {
            Data = dto.Data,
            MimeType = dto.MimeType,
            Extension = dto.Extension,
            FileName = dto.FileName
        };
    }
}

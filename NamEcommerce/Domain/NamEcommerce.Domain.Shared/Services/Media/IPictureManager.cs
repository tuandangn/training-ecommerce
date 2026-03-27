using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Domain.Shared.Services.Media;

public interface IPictureManager
{
    Task<PictureDto?> GetPictureByIdAsync(Guid id);

    Task<CreatePictureResultDto> CreatePictureAsync(CreatePictureDto dto);

    Task DeletePictureAsync(Guid id);
}

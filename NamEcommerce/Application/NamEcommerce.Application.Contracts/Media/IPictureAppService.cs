using NamEcommerce.Application.Contracts.Dtos.Media;

namespace NamEcommerce.Application.Contracts.Media;

public interface IPictureAppService
{
    Task<Base64PictureAppDto?> GetBase64PictureByIdAsync(Guid id); 
    Task<Guid> CreatePictureAsync(CreatePictureAppDto dto);
}

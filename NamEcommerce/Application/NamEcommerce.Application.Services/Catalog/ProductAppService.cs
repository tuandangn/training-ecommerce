using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Services.Catalog;
using NamEcommerce.Domain.Shared.Services.Media;

namespace NamEcommerce.Application.Services.Catalog;

public sealed class ProductAppService : IProductAppService
{
    private readonly IProductManager _productManager;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<UnitMeasurement> _unitMeasurementDataReader;
    private readonly IPictureManager _pictureManager;

    public ProductAppService(IProductManager productManager, IEntityDataReader<Product> productDataReader,
        IPictureManager pictureManager, IEntityDataReader<UnitMeasurement> unitMeasurementDataReader)
    {
        _productManager = productManager;
        _productDataReader = productDataReader;
        _pictureManager = pictureManager;
        _unitMeasurementDataReader = unitMeasurementDataReader;
    }

    public async Task<CreateProductResultAppDto> CreateProductAsync(CreateProductAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new CreateProductResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        if (await _productManager.DoesNameExistAsync(dto.Name).ConfigureAwait(false))
        {
            return new CreateProductResultAppDto
            {
                Success = false,
                ErrorMessage = "Tên hàng hóa trùng lặp."
            };
        }

        if (dto.UnitMeasurementId.HasValue)
        {
            var unitMeasurement = await _unitMeasurementDataReader.GetByIdAsync(dto.UnitMeasurementId.Value).ConfigureAwait(false);
            if (unitMeasurement is null)
            {
                return new CreateProductResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Không tìm thấy đơn vị tính."
                };
            }
        }

        Guid? pictureId = null;
        if (dto.ImageFile is not null && dto.ImageFile.Data.Length > 0 && !string.IsNullOrEmpty(dto.ImageFile.MimeType))
        {
            var insertedPicture = await _pictureManager.CreatePictureAsync(new CreatePictureDto
            {
                Data = dto.ImageFile.Data,
                MimeType = dto.ImageFile.MimeType,
                Extension = dto.ImageFile.Extension,
                FileName = dto.ImageFile.FileName
            }).ConfigureAwait(false);

            pictureId = insertedPicture.CreatedId;
        }

        var createDto = new CreateProductDto
        {
            Name = dto.Name,
            ShortDesc = dto.ShortDesc,
            UnitMeasurementId = dto.UnitMeasurementId,
            Categories = dto.Categories.Select(item => new ProductCategoryDto(item.CategoryId, item.DisplayOrder)),
            Pictures = pictureId.HasValue ? [pictureId.Value] : [],
            TrackInventory = dto.TrackInventory
        };
        var result = await _productManager.CreateProductAsync(createDto).ConfigureAwait(false);


        return new CreateProductResultAppDto
        {
            Success = true,
            CreatedId = result.CreatedId
        };
    }

    public async Task<DeleteProductResultAppDto> DeleteProductAsync(DeleteProductAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var product = await _productDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (product == null)
        {
            return new DeleteProductResultAppDto
            {
                Success = false,
                ErrorMessage = "Không tìm thấy hàng hóa."
            };
        }

        await _productManager.DeleteProductAsync(dto.Id).ConfigureAwait(false);

        return new DeleteProductResultAppDto { Success = true };
    }

    public async Task<ProductAppDto?> GetProductByIdAsync(Guid id)
    {
        var product = await _productDataReader.GetByIdAsync(id).ConfigureAwait(false);
        return product?.ToDto();
    }

    public async Task<IEnumerable<ProductAppDto>> GetProductsByIdsAsync(IEnumerable<Guid> ids)
    {
        if (!ids.Any())
            return [];

        var products = await _productDataReader.GetByIdsAsync(ids).ConfigureAwait(false);
        return products.Select(p => p.ToDto());
    }

    public async Task<IPagedDataAppDto<ProductAppDto>> GetProductsAsync(
        string? keywords = null, bool onlyTrackInventory = false,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var pagedData = await _productManager.GetProductsAsync(pageIndex, pageSize, keywords, onlyTrackInventory).ConfigureAwait(false);
        var result = PagedDataAppDto.Create(
            pagedData.Select(product => product.ToDto()),
            pageIndex, pageSize, pagedData.PagerInfo.TotalCount);

        return result;
    }

    public async Task<UpdateProductResultAppDto> UpdateProductAsync(UpdateProductAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new UpdateProductResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var product = await _productDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (product == null)
        {
            return new UpdateProductResultAppDto
            {
                Success = false,
                ErrorMessage = "Không tìm thấy hàng hóa"
            };
        }

        if (await _productManager.DoesNameExistAsync(dto.Name, dto.Id).ConfigureAwait(false))
        {
            return new UpdateProductResultAppDto
            {
                Success = false,
                ErrorMessage = "Tên hàng hóa trùng lặp."
            };
        }

        if (dto.UnitMeasurementId.HasValue)
        {
            var unitMeasurement = await _unitMeasurementDataReader.GetByIdAsync(dto.UnitMeasurementId.Value).ConfigureAwait(false);
            if (unitMeasurement is null)
            {
                return new UpdateProductResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Không tìm thấy đơn vị tính."
                };
            }
        }

        Guid? pictureId = null;
        if (dto.ImageFile is not null && dto.ImageFile.Data.Length > 0 && !string.IsNullOrEmpty(dto.ImageFile.MimeType))
        {
            var insertedPicture = await _pictureManager.CreatePictureAsync(new CreatePictureDto
            {
                Data = dto.ImageFile.Data,
                MimeType = dto.ImageFile.MimeType,
                Extension = dto.ImageFile.Extension,
                FileName = dto.ImageFile.FileName
            }).ConfigureAwait(false);

            pictureId = insertedPicture.CreatedId;
        }

        var result = await _productManager.UpdateProductAsync(new UpdateProductDto(dto.Id)
        {
            Name = dto.Name,
            ShortDesc = dto.ShortDesc,
            UnitMeasurementId = dto.UnitMeasurementId,
            Categories = dto.Categories.Select(pc => new ProductCategoryDto(pc.CategoryId, pc.DisplayOrder)),
            Pictures = pictureId.HasValue ? [pictureId.Value] : [],
            TrackInventory = dto.TrackInventory
        }).ConfigureAwait(false);

        return new UpdateProductResultAppDto
        {
            Success = true,
            UpdatedId = result.Id
        };
    }
}

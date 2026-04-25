using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Services.Catalog;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Media;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.Catalog;

public sealed class ProductAppService : IProductAppService
{
    private readonly IProductManager _productManager;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<UnitMeasurement> _unitMeasurementDataReader;
    private readonly IPictureManager _pictureManager;
    private readonly IEntityDataReader<Warehouse> _warehouseDataReader;
    private readonly IInventoryStockManager _inventoryStockManager;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public ProductAppService(IProductManager productManager, IEntityDataReader<Product> productDataReader,
        IPictureManager pictureManager, IEntityDataReader<UnitMeasurement> unitMeasurementDataReader,
        IEntityDataReader<Warehouse> warehouseDataReader, IInventoryStockManager inventoryStockManager,
        ICurrentUserAccessor currentUserAccessor)
    {
        _productManager = productManager;
        _productDataReader = productDataReader;
        _pictureManager = pictureManager;
        _unitMeasurementDataReader = unitMeasurementDataReader;
        _warehouseDataReader = warehouseDataReader;
        _inventoryStockManager = inventoryStockManager;
        _currentUserAccessor = currentUserAccessor;
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
                ErrorMessage = "Error.ProductNameAlreadyExists"
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
                    ErrorMessage = "Error.UnitMeasurementIsNotFound"
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

        if (dto.ProductStocks.Any())
        {
            if (!dto.UnitPrice.HasValue)
            {
                return new CreateProductResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.UnitPriceRequired"
                };
            }
            if (!dto.CostPrice.HasValue)
            {
                return new CreateProductResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.CostPriceRequired"
                };
            }
        }
        foreach (var productStock in dto.ProductStocks)
        {
            if (!productStock.WarehouseId.HasValue)
                continue;

            var warehouse = await _warehouseDataReader.GetByIdAsync(productStock.WarehouseId.Value);
            if (warehouse is null)
            {
                return new CreateProductResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.WarehouseIsNotFound"
                };
            }
        }

        var createProductDto = new CreateProductDto
        {
            Name = dto.Name,
            ShortDesc = dto.ShortDesc,
            UnitMeasurementId = dto.UnitMeasurementId,
            Categories = dto.Categories.Select(item => new ProductCategoryDto(item.CategoryId, item.DisplayOrder)),
            Vendors = dto.Vendors.Select(item => new ProductVendorDto(item.VendorId, item.DisplayOrder)),
            Pictures = pictureId.HasValue ? [pictureId.Value] : [],

        };
        var creationResult = await _productManager.CreateProductAsync(createProductDto).ConfigureAwait(false);

        if (dto.ProductStocks.Any())
        {
            await _productManager.UpdateProductPriceAsync(new UpdateProductPriceDto(creationResult.CreatedId)
            {
                UnitCost = dto.CostPrice!.Value,
                UnitPrice = dto.UnitPrice!.Value,
                ChangePriceReason = "Giá bán khi tạo mới hàng hóa"
            });

            var currentUser = await _currentUserAccessor.GetCurrentUserAsync();
            foreach (var productStock in dto.ProductStocks)
            {
                if (!productStock.WarehouseId.HasValue)
                    continue;

                await _inventoryStockManager.AdjustStockAsync(creationResult.CreatedId, productStock.WarehouseId.Value,
                    productStock.Quantity, "Số lượng khi tạo hàng hóa", currentUser?.Id ?? Guid.Empty);
            }

        }

        return new CreateProductResultAppDto
        {
            Success = true,
            CreatedId = creationResult.CreatedId
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
                ErrorMessage = "Error.ProductIsNotFound"
            };
        }

        await _productManager.DeleteProductAsync(dto.Id).ConfigureAwait(false);

        return new DeleteProductResultAppDto { Success = true };
    }

    public async Task<ProductAppDto?> GetProductByIdAsync(Guid id)
    {
        var product = await _productDataReader.GetByIdAsync(id).ConfigureAwait(false);

        if (product is null)
            return null;
        return product.ToDto();
    }

    public async Task<IEnumerable<ProductAppDto>> GetProductsByIdsAsync(IEnumerable<Guid> ids)
    {
        if (!ids.Any())
            return [];

        var products = await _productDataReader.GetByIdsAsync(ids).ConfigureAwait(false);

        return products.Select(p => p.ToDto());
    }

    public async Task<IEnumerable<ProductAppDto>> GetProductsByVendorIdAsync(Guid vendorId)
    {
        var products = await _productManager.GetProductsByVendorIdAsync(vendorId).ConfigureAwait(false);
        return products.Select(p => p.ToDto());
    }

    public async Task<IPagedDataAppDto<ProductAppDto>> GetProductsAsync(
        string? keywords = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var pagedData = await _productManager.GetProductsAsync(pageIndex, pageSize, keywords).ConfigureAwait(false);

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
                ErrorMessage = "Error.ProductIsNotFound"
            };
        }

        if (await _productManager.DoesNameExistAsync(dto.Name, dto.Id).ConfigureAwait(false))
        {
            return new UpdateProductResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.ProductNameAlreadyExists"
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
                    ErrorMessage = "Error.UnitMeasurementIsNotFound"
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
            Vendors = dto.Vendors.Select(pv => new ProductVendorDto(pv.VendorId, pv.DisplayOrder)),
            Pictures = pictureId.HasValue ? [pictureId.Value] : []
        }).ConfigureAwait(false);

        if (dto.NewUnitPrice.HasValue)
        {
            await _productManager.UpdateProductPriceAsync(new UpdateProductPriceDto(product.Id)
            {
                UnitCost = product.CostPrice,
                UnitPrice = dto.NewUnitPrice.Value,
                ChangePriceReason = dto.ChangePriceReason
            }).ConfigureAwait(false);
        }

        return new UpdateProductResultAppDto
        {
            Success = true,
            UpdatedId = result.Id
        };
    }

    public async Task<IEnumerable<ProductPriceHistoryAppDto>> GetProductPriceHistoryAsync(Guid productId)
    {
        var history = await _productManager.GetProductPriceHistoryAsync(productId).ConfigureAwait(false);
        return history.Select(h => new ProductPriceHistoryAppDto
        {
            Id = h.Id,
            OldPrice = h.OldPrice,
            NewPrice = h.NewPrice,
            OldCostPrice = h.OldCostPrice,
            NewCostPrice = h.NewCostPrice,
            Note = h.Note,
            CreatedOnUtc = h.CreatedOnUtc
        });
    }
}

using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Domain.Services.Inventory;

public sealed class InventoryValidator : IInventoryValidator
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Warehouse> _warehouseRepository;

    public InventoryValidator(
        IRepository<Product> productRepository,
        IRepository<Warehouse> warehouseRepository)
    {
        _productRepository = productRepository;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<bool> ValidateProductExistsAsync(Guid productId)
    {
        if (productId == Guid.Empty)
            return false;

        var product = await _productRepository.GetByIdAsync(productId);
        return product != null;
    }

    public async Task<bool> ValidateWarehouseExistsAsync(Guid warehouseId)
    {
        if (warehouseId == Guid.Empty)
            return false;

        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId);
        return warehouse != null;
    }

    public async Task ValidateStockOperationAsync(Guid productId, Guid warehouseId, decimal quantity)
    {
        // Validate IDs are not empty
        if (productId == Guid.Empty)
            throw new InvalidStockOperationException("Mã sản phẩm không được để trống");

        if (warehouseId == Guid.Empty)
            throw new InvalidStockOperationException("Mã kho không được để trống");

        // Validate quantity
        if (quantity <= 0)
            throw new InvalidStockOperationException("Số lượng phải lớn hơn 0");

        // Check if product exists
        if (!await ValidateProductExistsAsync(productId))
            throw new InvalidStockOperationException($"Sản phẩm không tồn tại");

        // Check if warehouse exists  
        if (!await ValidateWarehouseExistsAsync(warehouseId))
            throw new InvalidStockOperationException($"Kho không tồn tại");
    }
}

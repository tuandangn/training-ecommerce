namespace NamEcommerce.Domain.Shared.Services.Inventory;

/// <summary>
/// Service for validating inventory operations and inputs
/// </summary>
public interface IInventoryValidator
{
    /// <summary>
    /// Validate that product exists and is active
    /// </summary>
    Task<bool> ValidateProductExistsAsync(Guid productId);

    /// <summary>
    /// Validate that warehouse exists and is active
    /// </summary>
    Task<bool> ValidateWarehouseExistsAsync(Guid warehouseId);

    /// <summary>
    /// Validate stock operation parameters
    /// Throws InvalidStockOperationException if validation fails
    /// </summary>
    Task ValidateStockOperationAsync(Guid productId, Guid warehouseId, decimal quantity);
}

namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class InsufficientStockException : Exception
{
    public InsufficientStockException(Guid productId, Guid warehouseId, decimal requestedQuantity, decimal availableQuantity) 
        : base($"Insufficient stock for product {productId} at warehouse {warehouseId}. Requested: {requestedQuantity}, Available: {availableQuantity}.")
    {
    }
}

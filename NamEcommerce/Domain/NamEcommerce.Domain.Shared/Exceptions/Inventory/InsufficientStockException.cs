namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class InsufficientStockException : NamEcommerceDomainException
{
    public InsufficientStockException(Guid productId, Guid warehouseId, decimal requestedQuantity, decimal availableQuantity) 
        : base("Error.InsufficientStock", productId, warehouseId, requestedQuantity, availableQuantity)
    {
    }
}


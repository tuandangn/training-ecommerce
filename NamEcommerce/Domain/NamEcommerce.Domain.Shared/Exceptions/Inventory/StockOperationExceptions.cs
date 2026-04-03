namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

/// <summary>
/// Thrown when stock operation would exceed warehouse capacity
/// </summary>
public sealed class WarehouseCapacityExceededException : Exception
{
    public WarehouseCapacityExceededException(string message) : base(message) { }
}

/// <summary>
/// Thrown when attempting to operate on a non-existent resource
/// </summary>
public sealed class StockNotFoundException : Exception
{
    public StockNotFoundException(string message) : base(message) { }
}

/// <summary>
/// Thrown when input validation fails
/// </summary>
public sealed class InvalidStockOperationException : Exception
{
    public InvalidStockOperationException(string message) : base(message) { }
}

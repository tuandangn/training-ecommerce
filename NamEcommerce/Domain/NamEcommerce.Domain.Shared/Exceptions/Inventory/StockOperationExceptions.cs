namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

/// <summary>
/// Thrown when stock operation would exceed warehouse capacity.
/// ErrorCode: "Error.WarehouseCapacityExceeded" — params: maxStockLevel, requestedQuantity
/// </summary>
[Serializable]
public sealed class WarehouseCapacityExceededException(string errorCode, params object[] parameters)
    : NamEcommerceDomainException(errorCode, parameters);

/// <summary>
/// Thrown when attempting to operate on a non-existent stock record.
/// ErrorCode: "Error.StockNotFound" — params: productId, warehouseId
/// </summary>
[Serializable]
public sealed class StockNotFoundException(string errorCode, params object[] parameters)
    : NamEcommerceDomainException(errorCode, parameters);

/// <summary>
/// Thrown when a stock operation input is invalid (quantity, ID checks, etc.).
/// ErrorCode: one of Error.StockQuantityCannotBeNegative, Error.StockQuantityMustBePositive, etc.
/// </summary>
[Serializable]
public sealed class InvalidStockOperationException(string errorCode, params object[] parameters)
    : NamEcommerceDomainException(errorCode, parameters);

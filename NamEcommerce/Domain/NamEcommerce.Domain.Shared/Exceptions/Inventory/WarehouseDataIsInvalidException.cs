namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class WarehouseDataIsInvalidException(string? message) : Exception(message);

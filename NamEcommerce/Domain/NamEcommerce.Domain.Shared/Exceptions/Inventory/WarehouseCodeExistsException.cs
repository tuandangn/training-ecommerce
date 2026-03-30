namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class WarehouseCodeExistsException(string code) : Exception($"Warehouse with code '{code}' exists");


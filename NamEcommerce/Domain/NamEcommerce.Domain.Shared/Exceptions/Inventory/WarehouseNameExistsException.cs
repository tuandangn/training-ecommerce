namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class WarehouseNameExistsException(string name) : Exception($"Warehouse with name '{name}' exists");


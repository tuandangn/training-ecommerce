namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class WarehouseIsNotFoundException(Guid id) : Exception($"Warehouse with id '{id}' is not found");

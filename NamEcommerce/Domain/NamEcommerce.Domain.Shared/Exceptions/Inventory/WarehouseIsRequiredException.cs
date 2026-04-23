namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class WarehouseIsRequiredException() : NamEcommerceDomainException("Error.Warehouse.Required");

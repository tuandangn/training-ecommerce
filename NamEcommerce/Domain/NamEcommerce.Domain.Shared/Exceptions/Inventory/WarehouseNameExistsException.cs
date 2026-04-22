namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class WarehouseNameExistsException(string name)  : NamEcommerceDomainException("Error.WarehouseNameExistsException", name);



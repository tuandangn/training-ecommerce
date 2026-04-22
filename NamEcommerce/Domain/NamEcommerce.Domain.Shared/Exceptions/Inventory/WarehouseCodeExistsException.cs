namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class WarehouseCodeExistsException(string code)  : NamEcommerceDomainException("Error.WarehouseCodeExistsException", code);



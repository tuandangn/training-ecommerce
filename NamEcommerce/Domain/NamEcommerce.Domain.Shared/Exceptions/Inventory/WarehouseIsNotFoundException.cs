namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class WarehouseIsNotFoundException(Guid id)  : NamEcommerceDomainException("Error.WarehouseIsNotFoundException", id);


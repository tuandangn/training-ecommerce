namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class WarehouseIsNotSuitableException(Guid id)  : NamEcommerceDomainException("Error.WarehouseIsNotSuitableException", id);



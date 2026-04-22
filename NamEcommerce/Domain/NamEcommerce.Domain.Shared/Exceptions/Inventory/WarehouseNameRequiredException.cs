namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class WarehouseNameRequiredException() : NamEcommerceDomainException("Error.WarehouseNameRequired");

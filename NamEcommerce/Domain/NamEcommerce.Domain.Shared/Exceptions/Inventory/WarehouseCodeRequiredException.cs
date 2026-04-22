namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class WarehouseCodeRequiredException() : NamEcommerceDomainException("Error.WarehouseCodeRequired");

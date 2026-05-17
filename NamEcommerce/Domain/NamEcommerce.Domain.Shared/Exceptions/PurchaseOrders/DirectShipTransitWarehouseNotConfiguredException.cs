namespace NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

[Serializable]
public sealed class DirectShipTransitWarehouseNotConfiguredException()
    : NamEcommerceDomainException("Error.DirectShip.TransitWarehouseNotConfigured");

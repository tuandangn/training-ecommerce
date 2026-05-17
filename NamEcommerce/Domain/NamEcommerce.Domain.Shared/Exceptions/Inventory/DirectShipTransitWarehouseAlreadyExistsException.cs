namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class DirectShipTransitWarehouseAlreadyExistsException()
    : NamEcommerceDomainException("Error.DirectShip.TransitWarehouseAlreadyExists");

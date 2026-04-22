namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class UnitMeasurementIsNotFoundException(Guid id)
    : NamEcommerceDomainException("Error.UnitMeasurementIsNotFound", id);

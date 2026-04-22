namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class UnitMeasurementNameRequiredException() : NamEcommerceDomainException("Error.UnitMeasurementNameRequired");

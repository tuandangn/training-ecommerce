namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class UnitMeasurementDataIsInvalidException(string? message)  : NamEcommerceDomainException("Error.UnitMeasurementDataIsInvalidException", message);


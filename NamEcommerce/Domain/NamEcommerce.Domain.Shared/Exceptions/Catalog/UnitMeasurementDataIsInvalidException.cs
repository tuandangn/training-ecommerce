namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class UnitMeasurementDataIsInvalidException(string errorCode, params object[] parameters) : NamEcommerceDomainException(errorCode, parameters);



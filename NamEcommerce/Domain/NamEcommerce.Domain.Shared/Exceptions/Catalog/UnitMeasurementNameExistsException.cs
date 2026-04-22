namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class UnitMeasurementNameExistsException(string name)  : NamEcommerceDomainException("Error.UnitMeasurementNameExistsException", name);


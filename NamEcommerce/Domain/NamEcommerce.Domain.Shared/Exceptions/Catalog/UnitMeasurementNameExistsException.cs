namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class UnitMeasurementNameExistsException(string name) : Exception($"Unit measurement with name '{name}' exists");

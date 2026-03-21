namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class UnitMeasurementNameExistsException : Exception
{
    public UnitMeasurementNameExistsException(string name) : base($"Unit measurement with name '{name}' exists")
    {
    }
}

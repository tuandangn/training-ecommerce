namespace NamEcommerce.Api.GraphQl.Models.Catalog;

[Serializable]
public sealed record UnitMeasurementModel
{
    public UnitMeasurementModel(Guid id, string name)
        => (Id, Name) = (id, name);

    public Guid Id { get; }
    public string Name { get; }
}

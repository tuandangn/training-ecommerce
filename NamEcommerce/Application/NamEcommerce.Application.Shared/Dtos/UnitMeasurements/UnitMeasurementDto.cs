namespace NamEcommerce.Application.Shared.Dtos.Catalog;

[Serializable]
public sealed record UnitMeasurementDto
{
    public UnitMeasurementDto(Guid id, string name)
        => (Id, Name) = (id, name);

    public Guid Id { get; set; }
    public string Name { get; set; }
    public int DisplayOrder { get; set; }
}

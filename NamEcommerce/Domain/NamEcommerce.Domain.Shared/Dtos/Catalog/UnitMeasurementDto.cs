namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public sealed record UnitMeasurementDto
{
    public UnitMeasurementDto(Guid id, string name)
        => (Id, Name) = (id, name);

    public Guid Id { get; init; }
    public string Name { get; init; }
    public int DisplayOrder { get; set; }
}

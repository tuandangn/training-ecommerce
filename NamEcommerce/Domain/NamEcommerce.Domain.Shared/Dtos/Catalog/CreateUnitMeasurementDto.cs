namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public sealed record CreateUnitMeasurementDto
{
    public CreateUnitMeasurementDto(string name)
        => Name = name  ;

    public string Name { get; init; }
    public int DisplayOrder { get; set; }
}

namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public abstract record BaseUnitMeasurementDto
{
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
}

[Serializable]
public sealed record UnitMeasurementDto(Guid Id) : BaseUnitMeasurementDto;

[Serializable]
public sealed record CreateUnitMeasurementDto : BaseUnitMeasurementDto;

[Serializable]
public sealed record CreateUnitMeasurementResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateUnitMeasurementDto(Guid Id) : BaseUnitMeasurementDto;
[Serializable]
public sealed record UpdateUnitMeasurementResultDto(Guid Id) : BaseUnitMeasurementDto;


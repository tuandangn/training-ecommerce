namespace NamEcommerce.Application.Contracts.Dtos.Catalog;

[Serializable]
public record BaseUnitMeasurementAppDto
{
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (string.IsNullOrEmpty(Name))
            return (false, "Error.UnitMeasurementNameRequired");

        return (true, null);
    }
}

[Serializable]
public sealed record UnitMeasurementAppDto(Guid Id) : BaseUnitMeasurementAppDto;

[Serializable]
public sealed record CreateUnitMeasurementAppDto : BaseUnitMeasurementAppDto;
[Serializable]
public sealed record CreateUnitMeasurementResultAppDto
{
    public required bool Success { get; init; }
    public Guid? CreatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record UpdateUnitMeasurementAppDto(Guid Id) : BaseUnitMeasurementAppDto;

[Serializable]
public sealed record UpdateUnitMeasurementResultAppDto
{
    public required bool Success { get; init; }
    public Guid UpdatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record DeleteUnitMeasurementAppDto(Guid Id);
[Serializable]
public sealed record DeleteUnitMeasurementResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}

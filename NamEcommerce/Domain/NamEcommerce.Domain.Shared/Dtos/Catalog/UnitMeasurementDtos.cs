using NamEcommerce.Domain.Shared.Exceptions.Catalog;

namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public abstract record BaseUnitMeasurementDto
{
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }

    public virtual void Verify()
    {
        if (string.IsNullOrEmpty(Name))
            throw new UnitMeasurementDataIsInvalidException("Error.UnitMeasurementNameRequired");
    }
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


using NamEcommerce.Web.Contracts.Models.UnitMeasurements;

namespace NamEcommerce.Web.Contracts.Commands.Models.Catalog;

[Serializable]
public sealed class CreateUnitMeasurementCommand : ICommand<CreateUnitMeasurementResultModel>
{
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
    public int DecimalPlaces { get; set; }
}

using MediatR;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;

namespace NamEcommerce.Web.Contracts.Commands.Models.Catalog;

[Serializable]
public sealed class CreateUnitMeasurementCommand : IRequest<CreateUnitMeasurementResultModel>
{
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
}

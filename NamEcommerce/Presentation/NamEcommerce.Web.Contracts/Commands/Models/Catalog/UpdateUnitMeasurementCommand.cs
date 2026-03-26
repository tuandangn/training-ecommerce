using MediatR;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;

namespace NamEcommerce.Web.Contracts.Commands.Models.Catalog;

[Serializable]
public sealed class UpdateUnitMeasurementCommand : IRequest<UpdateUnitMeasurementResultModel>
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
}

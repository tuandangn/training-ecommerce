using MediatR;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetUnitMeasurementQuery : IRequest<UnitMeasurementModel?>
{
    public required Guid Id { get; init; }
}

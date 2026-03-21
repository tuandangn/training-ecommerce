using MediatR;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;

namespace NamEcommerce.Web.Contracts.Commands.Models.Catalog
{
    [Serializable]
    public sealed record DeleteUnitMeasurementCommand(Guid Id) : IRequest<DeleteUnitMeasurementResultModel>;
}

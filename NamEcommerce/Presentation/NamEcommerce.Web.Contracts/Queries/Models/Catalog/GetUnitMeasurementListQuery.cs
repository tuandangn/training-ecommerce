using MediatR;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetUnitMeasurementListQuery : IRequest<UnitMeasurementListModel>
{
    public required string? Keywords { get; init; }

    public required int PageIndex { get; init; }
    public required int PageSize { get; init; }
}

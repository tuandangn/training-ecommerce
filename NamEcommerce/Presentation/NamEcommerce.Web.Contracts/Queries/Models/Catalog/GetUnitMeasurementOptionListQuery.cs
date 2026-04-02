using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Queries.Models.Catalog;

[Serializable]
public sealed class GetUnitMeasurementOptionListQuery : IRequest<EntityOptionListModel>
{
}

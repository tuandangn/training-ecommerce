using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Queries.Models.Inventory;

[Serializable]
public sealed class GetWarehouseTypeOptionsQuery : IRequest<CommonOptionListModel>
{
}

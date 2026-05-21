using MediatR;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Queries.Models.Inventory;

[Serializable]
public sealed class GetInventoryCostingPolicyQuery : IRequest<InventoryCostingPolicySettingsModel>
{
}

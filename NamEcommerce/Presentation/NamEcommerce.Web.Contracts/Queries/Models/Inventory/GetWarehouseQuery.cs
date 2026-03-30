using MediatR;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Queries.Models.Inventory;

[Serializable]
public sealed class GetWarehouseQuery : IRequest<WarehouseModel?>
{
    public required Guid Id { get; set; }
}

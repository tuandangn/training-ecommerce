using MediatR;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Queries.Models.Inventory;

public sealed class GetWarehouseByIdQuery : IRequest<WarehouseDetailModel?>
{
    public required Guid Id { get; init; }
}

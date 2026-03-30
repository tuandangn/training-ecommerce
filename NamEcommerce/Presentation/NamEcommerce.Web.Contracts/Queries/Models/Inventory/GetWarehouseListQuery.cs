using MediatR;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Queries.Models.Inventory;

[Serializable]
public sealed class GetWarehouseListQuery : IRequest<WarehouseListModel>
{
    public required string? Keywords { get; set; }

    public required int PageIndex { get; init; }
    public required int PageSize { get; init; }
}


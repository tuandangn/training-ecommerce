using MediatR;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Queries.Models.Inventory;

public sealed class GetInventoryStockListQuery : IRequest<InventoryStockListModel>
{
    public string? Keywords { get; init; }
    public Guid? WarehouseId { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
}

public sealed class GetStockMovementLogsQuery : IRequest<StockMovementLogListModel>
{
    public Guid? ProductId { get; init; }
    public Guid? WarehouseId { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
}

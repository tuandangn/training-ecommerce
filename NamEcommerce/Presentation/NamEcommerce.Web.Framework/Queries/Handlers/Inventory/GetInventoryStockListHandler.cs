using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Framework.Common;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Inventory;

public sealed class GetInventoryStockListHandler : IRequestHandler<GetInventoryStockListQuery, InventoryStockListModel>
{
    private readonly IInventoryAppService _inventoryAppService;

    public GetInventoryStockListHandler(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
    }

    public async Task<InventoryStockListModel> Handle(GetInventoryStockListQuery request, CancellationToken cancellationToken)
    {
        if (request.GroupByProduct)
        {
            var groupedData = await _inventoryAppService.GetInventoryStocksGroupedByProductAsync(
                request.PageIndex,
                request.PageSize,
                request.WarehouseId,
                request.Keywords,
                request.IncludeDirectTransit);

            return new InventoryStockListModel
            {
                Keywords = request.Keywords,
                WarehouseId = request.WarehouseId,
                IncludeDirectTransit = request.IncludeDirectTransit,
                GroupByProduct = true,
                Data = PagedDataModel.Create(Array.Empty<InventoryStockListModel.ItemModel>()),
                GroupedData = groupedData.MapToModel(MapGroupedItem)
            };
        }

        var pagedData = await _inventoryAppService.GetInventoryStocksAsync(request.PageIndex, request.PageSize, request.WarehouseId, null, request.Keywords, request.IncludeDirectTransit);

        var model = new InventoryStockListModel
        {
            Keywords = request.Keywords,
            WarehouseId = request.WarehouseId,
            IncludeDirectTransit = request.IncludeDirectTransit,
            GroupByProduct = false,
            Data = pagedData.MapToModel(MapItem),
            GroupedData = PagedDataModel.Create(Array.Empty<InventoryStockListModel.GroupedItemModel>())
        };

        return model;
    }

    private static InventoryStockListModel.ItemModel MapItem(InventoryStockAppDto item)
        => new(item.Id)
        {
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            WarehouseId = item.WarehouseId,
            WarehouseName = item.WarehouseName,
            QuantityOnHand = item.QuantityOnHand,
            QuantityReserved = item.QuantityReserved,
            TotalReservedByOrder = item.TotalReservedByOrder,
            QuantityAvailable = item.QuantityAvailable,
            CurrentUnitCost = item.CurrentUnitCost,
            UpdatedOn = DateTimeHelper.ToLocalTime(item.UpdatedOnUtc),
            ReorderLevel = item.ReorderLevel,
            MaxStockLevel = item.MaxStockLevel,
            CostHistory = item.CostHistory.Select(MapCostHistory).ToList()
        };

    private static InventoryStockListModel.GroupedItemModel MapGroupedItem(InventoryStockByProductAppDto item)
        => new(item.ProductId)
        {
            ProductName = item.ProductName,
            QuantityOnHand = item.QuantityOnHand,
            QuantityReserved = item.QuantityReserved,
            TotalReservedByOrder = item.TotalReservedByOrder,
            QuantityAvailable = item.QuantityAvailable,
            CurrentUnitCost = item.CurrentUnitCost,
            UpdatedOn = DateTimeHelper.ToLocalTime(item.UpdatedOnUtc),
            Warehouses = item.Warehouses.Select(MapItem).ToList(),
            CostHistory = item.CostHistory.Select(MapCostHistory).ToList()
        };

    private static InventoryStockListModel.CostHistoryItemModel MapCostHistory(InventoryCostHistoryAppDto item)
        => new(item.Id)
        {
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            WarehouseId = item.WarehouseId,
            WarehouseName = item.WarehouseName,
            OccurredAt = DateTimeHelper.ToLocalTime(item.OccurredAtUtc),
            SequenceNumber = item.SequenceNumber,
            MovementType = item.MovementType,
            QuantityDelta = item.QuantityDelta,
            UnitCost = item.UnitCost,
            TotalCost = item.TotalCost,
            QuantityBalanceAfter = item.QuantityBalanceAfter,
            ValueBalanceAfter = item.ValueBalanceAfter,
            AverageCostAfter = item.AverageCostAfter,
            CostingStatus = item.CostingStatus,
            ReferenceType = item.ReferenceType,
            ReferenceId = item.ReferenceId,
            ReferenceItemId = item.ReferenceItemId
        };
}

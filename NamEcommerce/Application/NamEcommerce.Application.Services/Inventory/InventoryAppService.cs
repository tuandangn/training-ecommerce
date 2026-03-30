using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Inventory;

public sealed class InventoryAppService : IInventoryAppService
{
    private readonly IInventoryStockManager _stockManager;

    public InventoryAppService(IInventoryStockManager stockManager)
    {
        _stockManager = stockManager;
    }

    public async Task<IPagedDataAppDto<InventoryStockAppDto>> GetInventoryStocksAsync(string? keywords, Guid? warehouseId, int pageIndex, int pageSize)
    {
        var (total, dataItems) = await _stockManager.GetInventoryStocksAsync(keywords, warehouseId, pageIndex, pageSize);

        var items = dataItems.Select(x => new InventoryStockAppDto
        {
            Id = x.Id,
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            WarehouseId = x.WarehouseId,
            WarehouseName = x.WarehouseName,
            QuantityOnHand = x.QuantityOnHand,
            QuantityReserved = x.QuantityReserved,
            QuantityAvailable = x.QuantityAvailable,
            UpdatedOnUtc = x.UpdatedOnUtc
        }).ToList();

        return PagedDataAppDto.Create(items, pageIndex, pageSize, total);
    }

    public async Task<IPagedDataAppDto<StockMovementLogAppDto>> GetStockMovementLogsAsync(Guid? productId, Guid? warehouseId, int pageIndex, int pageSize)
    {
        var (total, dataItems) = await _stockManager.GetStockMovementLogsAsync(productId, warehouseId, pageIndex, pageSize);

        var items = dataItems.Select(x => new StockMovementLogAppDto
        {
            Id = x.Id,
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            MovementType = x.MovementType,
            Quantity = x.Quantity,
            QuantityBefore = x.QuantityBefore,
            QuantityAfter = x.QuantityAfter,
            CreatedOnUtc = x.CreatedOnUtc,
            Note = x.Note
        }).ToList();

        return PagedDataAppDto.Create(items, pageIndex, pageSize, total);
    }

    public async Task<ResultAppDto> AdjustStockAsync(AdjustStockAppDto dto)
    {
        try
        {
            await _stockManager.AdjustStockAsync(dto.ProductId, dto.WarehouseId, dto.NewQuantity, dto.Note, dto.ModifiedByUserId);

            return new ResultAppDto { Success = true, ErrorMessage = null };
        }
        catch (Exception ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }
}

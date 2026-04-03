using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Application.Services.Inventory;

public sealed class InventoryAppService : IInventoryAppService
{
    private readonly IInventoryStockManager _stockManager;
    private readonly IInventoryValidator _validator;

    public InventoryAppService(
        IInventoryStockManager stockManager,
        IInventoryValidator validator)
    {
        _stockManager = stockManager;
        _validator = validator;
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
            await _validator.ValidateStockOperationAsync(dto.ProductId, dto.WarehouseId, dto.NewQuantity);
            await _stockManager.AdjustStockAsync(dto.ProductId, dto.WarehouseId, dto.NewQuantity, dto.Note, dto.ModifiedByUserId);
            return new ResultAppDto { Success = true, ErrorMessage = null };
        }
        catch (InvalidStockOperationException ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (WarehouseCapacityExceededException ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = $"Cảnh báo vượt quá dung lượng kho: {ex.Message}" };
        }
        catch (Exception ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = "Lỗi khi điều chỉnh tồn kho. Vui lòng thử lại." };
        }
    }

    public async Task<ResultAppDto> ReserveStockAsync(ReserveStockAppDto dto)
    {
        try
        {
            await _validator.ValidateStockOperationAsync(dto.ProductId, dto.WarehouseId, dto.Quantity);
            await _stockManager.ReserveStockAsync(dto.ProductId, dto.WarehouseId, dto.Quantity, dto.ReferenceId, dto.UserId, dto.Note);
            return new ResultAppDto { Success = true, ErrorMessage = null };
        }
        catch (InsufficientStockException ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = $"Không đủ hàng: {ex.Message}" };
        }
        catch (InvalidStockOperationException ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = "Lỗi khi giữ hàng. Vui lòng thử lại." };
        }
    }

    public async Task<ResultAppDto> ReleaseReservedStockAsync(ReleaseStockAppDto dto)
    {
        try
        {
            await _validator.ValidateStockOperationAsync(dto.ProductId, dto.WarehouseId, dto.Quantity);
            await _stockManager.ReleaseReservedStockAsync(dto.ProductId, dto.WarehouseId, dto.Quantity, dto.ReferenceId, dto.UserId, dto.Note);
            return new ResultAppDto { Success = true, ErrorMessage = null };
        }
        catch (StockNotFoundException ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (InvalidStockOperationException ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = "Lỗi khi giải phóng hàng. Vui lòng thử lại." };
        }
    }

    public async Task<ResultAppDto> DispatchStockAsync(DispatchStockAppDto dto)
    {
        try
        {
            await _validator.ValidateStockOperationAsync(dto.ProductId, dto.WarehouseId, dto.Quantity);
            await _stockManager.DispatchStockAsync(dto.ProductId, dto.WarehouseId, dto.Quantity, dto.ReferenceId, dto.UserId, dto.Note);
            return new ResultAppDto { Success = true, ErrorMessage = null };
        }
        catch (InsufficientStockException ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = $"Không đủ hàng để xuất: {ex.Message}" };
        }
        catch (InvalidStockOperationException ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = "Lỗi khi xuất kho. Vui lòng thử lại." };
        }
    }

    public async Task<ResultAppDto> ReceiveStockAsync(ReceiveStockAppDto dto)
    {
        try
        {
            await _validator.ValidateStockOperationAsync(dto.ProductId, dto.WarehouseId, dto.Quantity);
            await _stockManager.ReceiveStockAsync(dto.ProductId, dto.WarehouseId, dto.Quantity, dto.Note, dto.UserId, dto.ReferenceType, dto.ReferenceId);
            return new ResultAppDto { Success = true, ErrorMessage = null };
        }
        catch (WarehouseCapacityExceededException ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = $"Vượt quá dung lượng kho: {ex.Message}" };
        }
        catch (InvalidStockOperationException ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new ResultAppDto { Success = false, ErrorMessage = "Lỗi khi nhập kho. Vui lòng thử lại." };
        }
    }
}

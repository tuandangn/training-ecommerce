using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.StockAdjustment;

namespace NamEcommerce.Application.Contracts.StockAdjustment;

public interface IStockAdjustmentNoteAppService
{
    Task<CreateStockAdjustmentNoteResultAppDto> CreateAsync(CreateStockAdjustmentNoteAppDto dto);
    Task<(bool Success, string? Error)> ApproveAsync(Guid id);
    Task<(bool Success, string? Error)> CancelAsync(Guid id);
    Task<StockAdjustmentNoteAppDto?> GetByIdAsync(Guid id);
    Task<IPagedDataAppDto<StockAdjustmentNoteListAppDto>> GetListAsync(
        int pageIndex, int pageSize, string? keywords, Guid? warehouseId, int? status);
}

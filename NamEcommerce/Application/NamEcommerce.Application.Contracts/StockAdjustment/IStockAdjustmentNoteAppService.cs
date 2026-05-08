using NamEcommerce.Application.Contracts.Dtos.StockAdjustment;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Enums.StockAdjustment;

namespace NamEcommerce.Application.Contracts.StockAdjustment;

public interface IStockAdjustmentNoteAppService
{
    Task<CreateStockAdjustmentNoteResultAppDto> CreateAsync(CreateStockAdjustmentNoteAppDto dto, Guid? createdByUserId);
    Task<(bool Success, string? Error)> ApproveAsync(Guid id);
    Task<(bool Success, string? Error)> CancelAsync(Guid id);
    Task<StockAdjustmentNoteAppDto?> GetByIdAsync(Guid id);
    Task<IPagedDataDto<StockAdjustmentNoteListAppDto>> GetListAsync(
        int pageIndex, int pageSize, string? keywords, Guid? warehouseId, StockAdjustmentStatus? status);
}

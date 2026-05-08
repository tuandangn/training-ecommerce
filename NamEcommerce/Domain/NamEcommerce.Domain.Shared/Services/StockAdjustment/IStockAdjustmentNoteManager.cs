using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.StockAdjustment;
using NamEcommerce.Domain.Shared.Enums.StockAdjustment;

namespace NamEcommerce.Domain.Shared.Services.StockAdjustment;

public interface IStockAdjustmentNoteManager
{
    Task<StockAdjustmentNoteDto> CreateAsync(CreateStockAdjustmentNoteDto dto, Guid? createdByUserId);
    Task ApproveAsync(Guid id);
    Task CancelAsync(Guid id);
    Task<StockAdjustmentNoteDto?> GetByIdAsync(Guid id);
    Task<IPagedDataDto<StockAdjustmentNoteDto>> GetListAsync(
        int pageIndex, int pageSize, string? keywords, Guid? warehouseId,
        StockAdjustmentStatus? status);
}

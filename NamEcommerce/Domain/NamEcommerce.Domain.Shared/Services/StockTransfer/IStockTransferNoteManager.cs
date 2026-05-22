using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.StockTransfer;
using NamEcommerce.Domain.Shared.Enums.StockTransfer;

namespace NamEcommerce.Domain.Shared.Services.StockTransfer;

public interface IStockTransferNoteManager
{
    Task<StockTransferNoteDto> CreateAsync(CreateStockTransferNoteDto dto);
    Task ApproveAsync(Guid id);
    Task CancelAsync(Guid id);
    Task<StockTransferNoteDto?> GetByIdAsync(Guid id);
    Task<IPagedDataDto<StockTransferNoteDto>> GetListAsync(int pageIndex, int pageSize, string? keywords, Guid? fromWarehouseId, StockTransferStatus? status);
}

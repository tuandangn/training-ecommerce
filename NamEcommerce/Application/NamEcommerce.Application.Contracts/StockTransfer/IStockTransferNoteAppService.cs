using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.StockTransfer;

namespace NamEcommerce.Application.Contracts.StockTransfer;

public interface IStockTransferNoteAppService
{
    Task<CreateStockTransferNoteResultAppDto> CreateAsync(CreateStockTransferNoteAppDto dto);
    Task<StockTransferNoteResultAppDto> ApproveAsync(Guid id);
    Task<StockTransferNoteResultAppDto> CancelAsync(Guid id);
    Task<StockTransferNoteAppDto?> GetByIdAsync(Guid id);
    Task<IPagedDataAppDto<StockTransferNoteListAppDto>> GetListAsync(int pageIndex, int pageSize, string? keywords, Guid? fromWarehouseId, int? status);
}

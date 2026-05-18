using NamEcommerce.Web.Contracts.Models.StockTransfer;
using NamEcommerce.Web.Models.StockTransfer;

namespace NamEcommerce.Web.Services.StockTransfer;

public interface IStockTransferNoteModelFactory
{
    Task<CreateStockTransferNoteModel> PrepareCreateModelAsync();
    Task<StockTransferNoteModel?> PrepareDetailsModelAsync(Guid id);
    Task<StockTransferNoteListModel> PrepareListModelAsync(int pageNumber, int pageSize, string? keywords, Guid? fromWarehouseId, int? status);
}

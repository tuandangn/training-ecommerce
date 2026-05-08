using NamEcommerce.Web.Contracts.Models.StockAdjustment;
using NamEcommerce.Web.Models.StockAdjustment;

namespace NamEcommerce.Web.Services.StockAdjustment;

public interface IStockAdjustmentNoteModelFactory
{
    Task<CreateStockAdjustmentNoteModel> PrepareCreateModelAsync();
    Task<StockAdjustmentNoteModel?> PrepareDetailsModelAsync(Guid id);
    Task<StockAdjustmentNoteListModel> PrepareListModelAsync(
        int pageNumber, int pageSize, string? keywords, Guid? warehouseId, int? status);
}

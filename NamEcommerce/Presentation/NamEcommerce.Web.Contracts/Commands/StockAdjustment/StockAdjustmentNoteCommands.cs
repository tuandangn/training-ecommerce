using NamEcommerce.Web.Contracts.Models.StockAdjustment;

namespace NamEcommerce.Web.Contracts.Commands.StockAdjustment;

public sealed record CreateStockAdjustmentNoteCommand(
    Guid WarehouseId,
    string? Note,
    List<StockAdjustmentNoteItemInputModel> Items
) : ICommand<CreateStockAdjustmentNoteResultModel>;

public sealed record ApproveStockAdjustmentNoteCommand(Guid Id) : ICommand<StockAdjustmentNoteActionResultModel>;
public sealed record CancelStockAdjustmentNoteCommand(Guid Id) : ICommand<StockAdjustmentNoteActionResultModel>;

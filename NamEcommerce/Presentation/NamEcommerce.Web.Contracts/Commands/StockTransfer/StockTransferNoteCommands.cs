using NamEcommerce.Web.Contracts.Models.StockTransfer;

namespace NamEcommerce.Web.Contracts.Commands.StockTransfer;

public sealed record CreateStockTransferNoteCommand(
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    string? Note,
    List<StockTransferNoteItemInputModel> Items
) : ICommand<CreateStockTransferNoteResultModel>;

public sealed record ApproveStockTransferNoteCommand(Guid Id) : ICommand<StockTransferNoteActionResultModel>;
public sealed record CancelStockTransferNoteCommand(Guid Id) : ICommand<StockTransferNoteActionResultModel>;

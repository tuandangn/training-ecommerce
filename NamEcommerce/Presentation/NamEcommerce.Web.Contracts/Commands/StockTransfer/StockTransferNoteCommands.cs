using MediatR;
using NamEcommerce.Web.Contracts.Models.StockTransfer;

namespace NamEcommerce.Web.Contracts.Commands.StockTransfer;

public sealed record CreateStockTransferNoteCommand(
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    string? Note,
    List<StockTransferNoteItemInputModel> Items
) : IRequest<CreateStockTransferNoteResultModel>;

public sealed record ApproveStockTransferNoteCommand(Guid Id) : IRequest<StockTransferNoteActionResultModel>;
public sealed record CancelStockTransferNoteCommand(Guid Id) : IRequest<StockTransferNoteActionResultModel>;

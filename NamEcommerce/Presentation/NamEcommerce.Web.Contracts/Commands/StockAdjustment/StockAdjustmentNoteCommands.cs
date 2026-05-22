using MediatR;
using NamEcommerce.Web.Contracts.Models.StockAdjustment;

namespace NamEcommerce.Web.Contracts.Commands.StockAdjustment;

public sealed record CreateStockAdjustmentNoteCommand(
    Guid WarehouseId,
    string? Note,
    List<StockAdjustmentNoteItemInputModel> Items
) : IRequest<CreateStockAdjustmentNoteResultModel>;

public sealed record ApproveStockAdjustmentNoteCommand(Guid Id) : IRequest<StockAdjustmentNoteActionResultModel>;
public sealed record CancelStockAdjustmentNoteCommand(Guid Id) : IRequest<StockAdjustmentNoteActionResultModel>;

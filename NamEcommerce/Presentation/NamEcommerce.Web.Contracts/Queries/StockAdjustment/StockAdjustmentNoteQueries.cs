using MediatR;
using NamEcommerce.Domain.Shared.Enums.StockAdjustment;
using NamEcommerce.Web.Contracts.Models.StockAdjustment;

namespace NamEcommerce.Web.Contracts.Queries.StockAdjustment;

public sealed record GetStockAdjustmentNoteQuery(Guid Id) : IRequest<StockAdjustmentNoteModel?>;

public sealed record GetStockAdjustmentNoteListQuery(
    int PageNumber,
    int PageSize,
    string? Keywords,
    Guid? WarehouseId,
    StockAdjustmentStatus? Status
) : IRequest<StockAdjustmentNoteListModel>;

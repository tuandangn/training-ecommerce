using MediatR;
using NamEcommerce.Web.Contracts.Models.StockTransfer;

namespace NamEcommerce.Web.Contracts.Queries.StockTransfer;

public sealed record GetStockTransferNoteQuery(Guid Id) : IRequest<StockTransferNoteModel?>;

public sealed record GetStockTransferNoteListQuery(
    int PageNumber,
    int PageSize,
    string? Keywords,
    Guid? FromWarehouseId,
    int? Status
) : IRequest<StockTransferNoteListModel>;

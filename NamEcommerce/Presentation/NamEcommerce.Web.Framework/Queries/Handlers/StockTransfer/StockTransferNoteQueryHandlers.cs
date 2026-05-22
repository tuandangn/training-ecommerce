using MediatR;
using NamEcommerce.Application.Contracts.StockTransfer;
using NamEcommerce.Web.Contracts.Models.StockTransfer;
using NamEcommerce.Web.Contracts.Queries.StockTransfer;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.StockTransfer;

public sealed class GetStockTransferNoteHandler(IStockTransferNoteAppService appService)
    : IRequestHandler<GetStockTransferNoteQuery, StockTransferNoteModel?>
{
    public async Task<StockTransferNoteModel?> Handle(
        GetStockTransferNoteQuery request, CancellationToken cancellationToken)
    {
        var dto = await appService.GetByIdAsync(request.Id).ConfigureAwait(false);
        if (dto is null) return null;

        return new StockTransferNoteModel
        {
            Id = dto.Id,
            Code = dto.Code,
            FromWarehouseId = dto.FromWarehouseId,
            FromWarehouseName = dto.FromWarehouseName,
            ToWarehouseId = dto.ToWarehouseId,
            ToWarehouseName = dto.ToWarehouseName,
            Note = dto.Note,
            Status = dto.Status,
            ApprovedOnUtc = dto.ApprovedOnUtc.HasValue
                ? DateTimeHelper.ToLocalTime(dto.ApprovedOnUtc.Value)
                : null,
            CreatedOnUtc = DateTimeHelper.ToLocalTime(dto.CreatedOnUtc),
            Items = dto.Items.Select(i => new StockTransferNoteItemModel
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            }).ToList()
        };
    }
}

public sealed class GetStockTransferNoteListHandler(IStockTransferNoteAppService appService)
    : IRequestHandler<GetStockTransferNoteListQuery, StockTransferNoteListModel>
{
    public async Task<StockTransferNoteListModel> Handle(
        GetStockTransferNoteListQuery request, CancellationToken cancellationToken)
    {
        var paged = await appService.GetListAsync(
            request.PageNumber - 1, request.PageSize,
            request.Keywords, request.FromWarehouseId, request.Status).ConfigureAwait(false);

        return new StockTransferNoteListModel
        {
            TotalCount = paged.Pagination.TotalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Items = paged.Items.Select(x => new StockTransferNoteListItemModel
            {
                Id = x.Id,
                Code = x.Code,
                FromWarehouseName = x.FromWarehouseName,
                ToWarehouseName = x.ToWarehouseName,
                Status = x.Status,
                ItemCount = x.ItemCount,
                CreatedOnUtc = DateTimeHelper.ToLocalTime(x.CreatedOnUtc)
            }).ToList()
        };
    }
}

using MediatR;
using NamEcommerce.Application.Contracts.StockAdjustment;
using NamEcommerce.Web.Contracts.Models.StockAdjustment;
using NamEcommerce.Web.Contracts.Queries.StockAdjustment;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.StockAdjustment;

public sealed class GetStockAdjustmentNoteHandler(IStockAdjustmentNoteAppService appService)
    : IRequestHandler<GetStockAdjustmentNoteQuery, StockAdjustmentNoteModel?>
{
    public async Task<StockAdjustmentNoteModel?> Handle(
        GetStockAdjustmentNoteQuery request, CancellationToken cancellationToken)
    {
        var dto = await appService.GetByIdAsync(request.Id).ConfigureAwait(false);
        if (dto is null) return null;

        return new StockAdjustmentNoteModel
        {
            Id = dto.Id,
            Code = dto.Code,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            Note = dto.Note,
            Status = dto.Status,
            ApprovedOnUtc = dto.ApprovedOnUtc.HasValue
                ? DateTimeHelper.ToLocalTime(dto.ApprovedOnUtc.Value)
                : null,
            CreatedOnUtc = DateTimeHelper.ToLocalTime(dto.CreatedOnUtc),
            Items = dto.Items.Select(i => new StockAdjustmentNoteItemModel
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                SystemQuantity = i.SystemQuantity,
                PhysicalQuantity = i.PhysicalQuantity
            }).ToList()
        };
    }
}

public sealed class GetStockAdjustmentNoteListHandler(IStockAdjustmentNoteAppService appService)
    : IRequestHandler<GetStockAdjustmentNoteListQuery, StockAdjustmentNoteListModel>
{
    public async Task<StockAdjustmentNoteListModel> Handle(
        GetStockAdjustmentNoteListQuery request, CancellationToken cancellationToken)
    {
        var paged = await appService.GetListAsync(
            request.PageNumber - 1, request.PageSize,
            request.Keywords, request.WarehouseId, request.Status).ConfigureAwait(false);

        return new StockAdjustmentNoteListModel
        {
            TotalCount = paged.Pagination.TotalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Items = paged.Items.Select(x => new StockAdjustmentNoteListItemModel
            {
                Id = x.Id,
                Code = x.Code,
                WarehouseName = x.WarehouseName,
                Status = (int)x.Status,
                ItemCount = x.ItemCount,
                CreatedOnUtc = DateTimeHelper.ToLocalTime(x.CreatedOnUtc)
            }).ToList()
        };
    }
}

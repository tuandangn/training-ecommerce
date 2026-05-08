using MediatR;
using NamEcommerce.Domain.Shared.Enums.StockAdjustment;
using NamEcommerce.Web.Contracts.Models.StockAdjustment;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.StockAdjustment;
using NamEcommerce.Web.Models.StockAdjustment;

namespace NamEcommerce.Web.Services.StockAdjustment;

public sealed class StockAdjustmentNoteModelFactory(IMediator mediator) : IStockAdjustmentNoteModelFactory
{
    public async Task<CreateStockAdjustmentNoteModel> PrepareCreateModelAsync()
    {
        var warehouses = await mediator.Send(new GetWarehouseOptionListQuery()).ConfigureAwait(false);

        return new CreateStockAdjustmentNoteModel
        {
            AvailableWarehouses = warehouses.Items
                .Select(w => new WarehouseSelectItem { Id = w.Id, Name = w.Name })
                .ToList()
        };
    }

    public async Task<StockAdjustmentNoteModel?> PrepareDetailsModelAsync(Guid id)
        => await mediator.Send(new GetStockAdjustmentNoteQuery(id)).ConfigureAwait(false);

    public async Task<StockAdjustmentNoteListModel> PrepareListModelAsync(
        int pageNumber, int pageSize, string? keywords, Guid? warehouseId, int? status)
    {
        StockAdjustmentStatus? statusEnum = status.HasValue
            ? (StockAdjustmentStatus)status.Value
            : null;

        return await mediator.Send(new GetStockAdjustmentNoteListQuery(
            pageNumber, pageSize, keywords, warehouseId, statusEnum)).ConfigureAwait(false);
    }
}

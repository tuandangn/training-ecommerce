using MediatR;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Web.Contracts.Models.StockTransfer;
using NamEcommerce.Web.Contracts.Queries.StockTransfer;
using NamEcommerce.Web.Models.StockTransfer;

namespace NamEcommerce.Web.Services.StockTransfer;

public sealed class StockTransferNoteModelFactory(IMediator mediator, IWarehouseAppService warehouseAppService) : IStockTransferNoteModelFactory
{
    public async Task<CreateStockTransferNoteModel> PrepareCreateModelAsync()
    {
        var warehouses = await warehouseAppService.GetWarehousesAsync().ConfigureAwait(false);

        var transferable = warehouses.Items
            .Where(w => w.WarehouseType != (int)WarehouseType.DirectShipTransit)
            .Select(w => new TransferWarehouseSelectItem { Id = w.Id, Name = w.Name })
            .ToList();

        return new CreateStockTransferNoteModel { AvailableWarehouses = transferable };
    }

    public async Task<StockTransferNoteModel?> PrepareDetailsModelAsync(Guid id)
        => await mediator.Send(new GetStockTransferNoteQuery(id)).ConfigureAwait(false);

    public async Task<StockTransferNoteListModel> PrepareListModelAsync(
        int pageNumber, int pageSize, string? keywords, Guid? fromWarehouseId, int? status)
        => await mediator.Send(new GetStockTransferNoteListQuery(pageNumber, pageSize, keywords, fromWarehouseId, status)).ConfigureAwait(false);
}

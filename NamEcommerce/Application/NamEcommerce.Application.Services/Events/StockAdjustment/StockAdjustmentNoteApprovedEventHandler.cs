using MediatR;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.StockAdjustment;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.StockAdjustment;

namespace NamEcommerce.Application.Services.Events.StockAdjustment;

public sealed class StockAdjustmentNoteApprovedEventHandler(
    IStockAdjustmentNoteManager noteManager,
    IInventoryStockManager stockManager,
    IInventoryCostingManager inventoryCostingManager) : INotificationHandler<StockAdjustmentNoteApproved>
{
    public async Task Handle(StockAdjustmentNoteApproved notification, CancellationToken cancellationToken)
    {
        var note = await noteManager.GetByIdAsync(notification.NoteId).ConfigureAwait(false);
        if (note is null) return;

        foreach (var item in note.Items.Where(i => i.Delta != 0))
        {
            await stockManager.ApplyAdjustmentAsync(
                item.ProductId,
                note.WarehouseId,
                item.Delta,
                note.Id,
                note.CreatedByUserId).ConfigureAwait(false);

            if (item.Delta > 0)
            {
                await inventoryCostingManager.RegisterInboundAsync(new RegisterInventoryInboundCostDto
                {
                    ProductId = item.ProductId,
                    WarehouseId = note.WarehouseId,
                    Quantity = item.Delta,
                    UnitCost = null,
                    MovementType = InventoryCostMovementType.PositiveAdjustment,
                    ReferenceType = InventoryCostReferenceType.Adjustment,
                    ReferenceId = note.Id,
                    ReferenceItemId = item.Id,
                    OccurredAtUtc = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            else
            {
                await inventoryCostingManager.RegisterOutboundAsync(new RegisterInventoryOutboundCostDto
                {
                    ProductId = item.ProductId,
                    WarehouseId = note.WarehouseId,
                    Quantity = Math.Abs(item.Delta),
                    MovementType = InventoryCostMovementType.NegativeAdjustment,
                    ReferenceType = InventoryCostReferenceType.Adjustment,
                    ReferenceId = note.Id,
                    ReferenceItemId = item.Id,
                    OccurredAtUtc = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
        }
    }
}

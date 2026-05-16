using MediatR;
using NamEcommerce.Domain.Shared.Events.StockAdjustment;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.StockAdjustment;

namespace NamEcommerce.Application.Services.Events.StockAdjustment;

/// <summary>
/// Áp dụng delta tồn kho cho từng item khi StockAdjustmentNote được Approved.
/// Delta > 0 → tăng tồn; Delta &lt; 0 → giảm tồn; Delta = 0 → bỏ qua.
/// </summary>
public sealed class StockAdjustmentNoteApprovedEventHandler(
    IStockAdjustmentNoteManager noteManager,
    IInventoryStockManager stockManager) : INotificationHandler<StockAdjustmentNoteApproved>
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
        }
    }
}

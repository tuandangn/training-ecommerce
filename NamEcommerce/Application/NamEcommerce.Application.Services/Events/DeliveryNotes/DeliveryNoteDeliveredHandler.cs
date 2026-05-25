using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

/// <summary>
/// Khi phiếu giao hàng đã giao thành công — sinh công nợ khách hàng tương ứng (idempotent qua <c>DeliveryNoteId</c>).
/// </summary>
public sealed class DeliveryNoteDeliveredHandler(
    ICustomerDebtManager debtManager,
    IDeliveryNoteAppService deliveryNoteAppService) : INotificationHandler<DeliveryNoteDelivered>
{
    private readonly ICustomerDebtManager _debtManager = debtManager;
    private readonly IDeliveryNoteAppService _deliveryNoteAppService = deliveryNoteAppService;

    public async Task Handle(DeliveryNoteDelivered notification, CancellationToken cancellationToken)
    {
        // Event đã carry đủ thông tin, vẫn fetch lại để đảm bảo phiếu vẫn ở trạng thái Delivered.
        var deliveryNote = await _deliveryNoteAppService.GetByIdAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null) return;

        // Guard: phiếu xuất do trả NCC không sinh công nợ khách hàng.
        if (deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToVendorReturn) return;

        var createDebtDto = new CreateCustomerDebtDto
        {
            CustomerId = notification.CustomerId,
            DeliveryNoteId = notification.DeliveryNoteId,
            TotalAmount = notification.TotalAmount, // "phiếu đã xuất thì phải thu đủ"
            DueDateUtc = null
        };

        await _debtManager.CreateDebtFromDeliveryNoteAsync(createDebtDto).ConfigureAwait(false);
    }
}

public sealed class DeliveryNoteDeliveredStockHandler(
    IDeliveryNoteAppService deliveryNoteAppService,
    IInventoryStockManager stockManager,
    IInventoryCostingManager inventoryCostingManager) : INotificationHandler<DeliveryNoteDelivered>
{
    public async Task Handle(DeliveryNoteDelivered notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            return;

        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null) return;

        if (deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToCustomer && !deliveryNote.IsDirectShip)
            return; // Standard delivery notes are handled in ConfirmAsync

        var releaseReservedStock = deliveryNote.OrderId != Guid.Empty
            && (deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToCustomer || deliveryNote.SourceType == (int)DeliveryNoteSourceType.DirectShipToCustomer);

        foreach (var item in deliveryNote.Items)
        {
            await stockManager.DispatchStockUpToAsync(
                item.ProductId,
                deliveryNote.WarehouseId,
                deliveryNote.Items
                    .Where(i => i.ProductId == item.ProductId)
                    .Sum(i => i.Quantity),
                deliveryNote.Id,
                Guid.Empty,
                $"Xuất hàng cho phiếu xuất {deliveryNote.Code}",
                releaseReservedStock: releaseReservedStock,
                referenceType: deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToVendorReturn
                    ? (int)NamEcommerce.Domain.Entities.Inventory.StockReferenceType.VendorReturn
                    : (int)NamEcommerce.Domain.Entities.Inventory.StockReferenceType.SalesOrder).ConfigureAwait(false);

            await inventoryCostingManager.RegisterOutboundAsync(new RegisterInventoryOutboundCostDto
            {
                ProductId = item.ProductId,
                WarehouseId = deliveryNote.WarehouseId,
                Quantity = item.Quantity,
                MovementType = deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToVendorReturn
                    ? InventoryCostMovementType.VendorReturn
                    : InventoryCostMovementType.SaleDispatch,
                ReferenceType = deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToVendorReturn
                    ? InventoryCostReferenceType.VendorReturn
                    : InventoryCostReferenceType.SalesOrder,
                ReferenceId = deliveryNote.Id,
                ReferenceItemId = item.Id,
                OccurredAtUtc = deliveryNote.DeliveredOnUtc ?? DateTime.UtcNow
            }).ConfigureAwait(false);
        }
    }
}

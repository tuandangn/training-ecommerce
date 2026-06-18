using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

public sealed class DeliveryNoteDeliveredHandler(
    ICustomerDebtManager debtManager,
    ICustomerLedgerManager customerLedgerManager,
    IDeliveryNoteAppService deliveryNoteAppService) : INotificationHandler<DeliveryNoteDelivered>
{
    private readonly ICustomerDebtManager _debtManager = debtManager;
    private readonly ICustomerLedgerManager _customerLedgerManager = customerLedgerManager;
    private readonly IDeliveryNoteAppService _deliveryNoteAppService = deliveryNoteAppService;

    public async Task Handle(DeliveryNoteDelivered notification, CancellationToken cancellationToken)
    {
        var deliveryNote = await _deliveryNoteAppService.GetByIdAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return;

        if (deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToVendorReturn)
            return;

        // Fallback cho event cũ trong Outbox được serialize trước khi có DebtAmount.
        var fallbackDebtAmount = Math.Max(notification.AmountToCollect, deliveryNote.TotalAmount + deliveryNote.Surcharge);
        var debtAmount = notification.DebtAmount > 0 ? notification.DebtAmount : fallbackDebtAmount;
        if (debtAmount <= 0)
            return;

        await _debtManager.CreateDebtFromDeliveryNoteAsync(new CreateCustomerDebtDto
        {
            CustomerId = notification.CustomerId,
            DeliveryNoteId = notification.DeliveryNoteId,
            TotalAmount = debtAmount,
            DueDateUtc = null
        }).ConfigureAwait(false);

        var surcharge = deliveryNote.Surcharge;
        var chargeAmount = debtAmount - surcharge;

        if (chargeAmount > 0)
        {
            await _customerLedgerManager.RecordChargeAsync(new RecordCustomerLedgerChargeDto
            {
                CustomerId = notification.CustomerId,
                Amount = chargeAmount,
                ReferenceType = CustomerLedgerReferenceType.DeliveryNote,
                ReferenceId = notification.DeliveryNoteId,
                ReferenceCode = deliveryNote.Code,
                OccurredAtUtc = deliveryNote.DeliveredOnUtc ?? DateTime.UtcNow
            }).ConfigureAwait(false);
        }

        if (surcharge > 0)
        {
            await _customerLedgerManager.RecordSurchargeAsync(new RecordCustomerLedgerChargeDto
            {
                CustomerId = notification.CustomerId,
                Amount = surcharge,
                ReferenceType = CustomerLedgerReferenceType.DeliveryNote,
                ReferenceId = notification.DeliveryNoteId,
                ReferenceCode = deliveryNote.Code,
                OccurredAtUtc = deliveryNote.DeliveredOnUtc ?? DateTime.UtcNow
            }).ConfigureAwait(false);
        }
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
        if (deliveryNote is null)
            return;

        if (deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToCustomer && !deliveryNote.IsDirectShip)
            return;

        var releaseReservedStock = deliveryNote.OrderId != Guid.Empty
            && (deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToCustomer || deliveryNote.SourceType == (int)DeliveryNoteSourceType.DirectShipToCustomer);
        var referenceType = deliveryNote.SourceType == (int)DeliveryNoteSourceType.ToVendorReturn
            ? (int)NamEcommerce.Domain.Entities.Inventory.StockReferenceType.VendorReturn
            : (int)NamEcommerce.Domain.Entities.Inventory.StockReferenceType.SalesOrder;

        var dispatchGroups = deliveryNote.Items
            .GroupBy(item => new
            {
                item.ProductId,
                WarehouseId = item.WarehouseId
            })
            .Select(group => new
            {
                group.Key.ProductId,
                group.Key.WarehouseId,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        foreach (var group in dispatchGroups)
        {
            if (group.WarehouseId == Guid.Empty)
                continue;
            await stockManager.DispatchStockUpToAsync(
                group.ProductId,
                group.WarehouseId,
                group.Quantity,
                deliveryNote.Id,
                Guid.Empty,
                $"Xuat hang cho phieu xuat {deliveryNote.Code}",
                releaseReservedStock: releaseReservedStock,
                referenceType: referenceType).ConfigureAwait(false);
        }

        foreach (var item in deliveryNote.Items)
        {
            await inventoryCostingManager.RegisterOutboundAsync(new RegisterInventoryOutboundCostDto
            {
                ProductId = item.ProductId,
                WarehouseId = item.WarehouseId,
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

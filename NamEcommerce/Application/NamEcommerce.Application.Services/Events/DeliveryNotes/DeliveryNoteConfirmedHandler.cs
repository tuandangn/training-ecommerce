using MediatR;
using NamEcommerce.Application.Contracts.Communication;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Outbox;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

public sealed class DeliveryNoteConfirmedHandler(IEntityDataReader<DeliveryNote> deliveryNoteDataReader,
    IInventoryStockManager inventoryStockManager, IInventoryCostingManager inventoryCostingManager, IOutbox outbox) : INotificationHandler<DeliveryNoteConfirmed>
{
    public async Task Handle(DeliveryNoteConfirmed notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            return;

        var deliveryNote = await deliveryNoteDataReader.GetByIdAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return;

        if (deliveryNote.IsDirectShip)
            return;

        if (deliveryNote.SourceType != DeliveryNoteSourceType.ToCustomer && deliveryNote.SourceType != DeliveryNoteSourceType.DirectShipToCustomer)
            return;

        foreach (var item in deliveryNote.Items)
        {
            await inventoryStockManager.DispatchStockAsync(
                item.ProductId,
                deliveryNote.WarehouseId,
                item.Quantity,
                deliveryNote.Id,
                Guid.Empty,
                $"Xuất hàng cho phiếu xuất {deliveryNote.Code}",
                releaseReservedStock: true).ConfigureAwait(false);

            await inventoryCostingManager.RegisterOutboundAsync(new RegisterInventoryOutboundCostDto
            {
                ProductId = item.ProductId,
                WarehouseId = deliveryNote.WarehouseId,
                Quantity = item.Quantity,
                MovementType = InventoryCostMovementType.SaleDispatch,
                ReferenceType = InventoryCostReferenceType.SalesOrder,
                ReferenceId = deliveryNote.Id,
                ReferenceItemId = item.Id,
                OccurredAtUtc = DateTime.UtcNow
            }).ConfigureAwait(false);
        }

        //dispatch to n8n
        var integrationEvent = new DeliveryNoteConfirmedIntegrationEvent(notification.DeliveryNoteId);
        await outbox.AddAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class DeliveryNoteConfirmedIntegrationHandler
    : INotificationHandler<DeliveryNoteConfirmedIntegrationEvent>
{
    private readonly IN8nAppService _n8nAppService;

    public DeliveryNoteConfirmedIntegrationHandler(IN8nAppService n8nAppService)
    {
        ArgumentNullException.ThrowIfNull(n8nAppService);
        _n8nAppService = n8nAppService;
    }

    public async Task Handle(DeliveryNoteConfirmedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            return;

        await _n8nAppService.NotifyDeliveryNoteIsConfirmed(notification.DeliveryNoteId).ConfigureAwait(false);
    }
}


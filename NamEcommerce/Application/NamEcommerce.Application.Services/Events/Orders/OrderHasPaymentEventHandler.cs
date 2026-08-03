using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Services.Customers;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Events.Orders;

public sealed class OrderHasPaymentEventHandler(IOrderManager orderManager, IDeliveryNoteManager deliveryNoteManager, IOrderFulfillmentScheduleAppService orderFulfillmentScheduleAppService)
    : INotificationHandler<OrderHasPayment>
{
    public async Task Handle(OrderHasPayment notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var order = await orderManager.GetOrderByIdAsync(notification.OrderId).ConfigureAwait(false);

        if (order is null)
            return;

        if (!order.ProcessRequiresPayment)
            return;

        if (!order.CanProcess)
            return;

        var deliveryNotes = await deliveryNoteManager.GetDeliveryNotesAsync(0, int.MaxValue, string.Empty, orderId: order.Id, [DeliveryNoteStatus.Draft]).ConfigureAwait(false);
        if (deliveryNotes.PagerInfo.TotalCount == 0 && order.IsRetailWalkInCustomer)
        {
            await orderFulfillmentScheduleAppService.CreateDefaultSchedulesForOrderAsync(order.Id, null).ConfigureAwait(false);
            return;
        }

        foreach (var deliveryNote in deliveryNotes)
        {
            if (!deliveryNote.RequiresPaymentToConfirm)
                continue;

            if (deliveryNote.HasPaid)
                return;

            if (deliveryNote.IsDirectShip)
                return;

            await deliveryNoteManager.MarkAsOrderIsPaid(deliveryNote.Id).ConfigureAwait(false);
            await deliveryNoteManager.ConfirmAsync(deliveryNote.Id).ConfigureAwait(false);

            await orderManager.RequestDeliveryAsync(order.Id, deliveryNote.Id, DateTime.UtcNow).ConfigureAwait(false);
        }
    }
}

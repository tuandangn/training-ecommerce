using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Services.Orders;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Customers;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Events.Customers;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Specifications.Orders;

namespace NamEcommerce.Application.Services.Events.Customers;

public sealed class CustomerKindChangedEventHandler(
    IEntityDataReader<Order> orderDataReader, IOrderManager orderManager,
    IDeliveryNoteManager deliveryNoteManager, IOrderFulfillmentScheduleAppService orderFulfillmentScheduleAppService) 
    : INotificationHandler<CustomerKindChanged>
{
    public async Task Handle(CustomerKindChanged notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var customerOrders = await orderDataReader.GetListAsync(new OrdersOfBuyersSpec([notification.CustomerId])).ConfigureAwait(false);
        foreach (var order in customerOrders)
        {
            if (order.OrderStatus != OrderStatus.Pending)
                continue;

            await orderManager.UpdateOrderCustomerKindAsync(new UpdateOrderCustomerKindDto
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId
            }).ConfigureAwait(false);

            if (notification.OldKind is CustomerKind.RetailWalkIn && notification.NewKind is CustomerKind.Standard)
            {
                if (!order.PayOffRequired) //has delivery notes
                    continue;

                var deliveryNotes = await deliveryNoteManager.GetDeliveryNotesAsync(0, int.MaxValue, string.Empty, orderId: order.Id, [DeliveryNoteStatus.Draft]).ConfigureAwait(false);
                if (deliveryNotes.PagerInfo.TotalCount == 0)
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
    }
}

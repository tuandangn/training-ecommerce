using MediatR;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Events.Debts;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Events.Debts;

public sealed class CustomerLedgerEntryRecordedEventHandler(
    IEntityDataReader<CustomerLedgerEntry> customerLedgerEntryReader, IEntityDataReader<Order> orderReader,
    IEntityDataReader<CustomerPayment> customerPaymentReader, IOrderManager orderManager)
    : INotificationHandler<CustomerLedgerEntryRecorded>
{
    public async Task Handle(CustomerLedgerEntryRecorded notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var customerLedgeEntry = await customerLedgerEntryReader.GetByIdAsync(notification.EntryId).ConfigureAwait(false);
        if (customerLedgeEntry is null)
            return;

        if (customerLedgeEntry.EntryType != CustomerLedgerEntryType.Payment)
            return;

        if (customerLedgeEntry.ReferenceType != CustomerLedgerReferenceType.CustomerPayment || !customerLedgeEntry.ReferenceId.HasValue)
            return;

        var customerPayment = await customerPaymentReader.GetByIdAsync(customerLedgeEntry.ReferenceId.Value).ConfigureAwait(false);
        if (customerPayment is null)
            return;

        if (!customerPayment.OrderId.HasValue)
            return;

        var order = await orderReader.GetByIdAsync(customerPayment.OrderId.Value).ConfigureAwait(false);
        if (order is null)
            return;

        await orderManager.MarkOrderHasPayment(order.Id, Math.Abs(customerLedgeEntry.Amount), customerPayment.PaymentIntentId);
    }
}

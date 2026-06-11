using MediatR;
using NamEcommerce.Domain.Shared.Events.Debts;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Application.Services.Events.Debts;

/// <summary>
/// Khi CustomerRefund hoàn thành (tiền đã trả lại cho khách), đóng CustomerCreditNote tương ứng
/// để credit note không còn treo với RemainingAmount > 0 trên BalanceSheet.
/// </summary>
public sealed class CustomerRefundCompletedEventHandler(
    ICustomerRefundManager customerRefundManager,
    ICustomerDebtManager customerDebtManager) : INotificationHandler<CustomerRefundCompleted>
{
    public async Task Handle(CustomerRefundCompleted notification, CancellationToken cancellationToken)
    {
        var refund = await customerRefundManager.GetByIdAsync(notification.CustomerRefundId)
            .ConfigureAwait(false);
        if (refund is null) return;

        await customerDebtManager.ConsumeCreditNoteByRefundAsync(
            refund.CustomerReturnId,
            notification.Amount).ConfigureAwait(false);
    }
}

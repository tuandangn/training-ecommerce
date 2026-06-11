using MediatR;
using NamEcommerce.Domain.Shared.Events.Debts;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Application.Services.Events.Debts;

/// <summary>
/// Khi VendorRefund hoàn thành (đã thu tiền từ NCC), đóng VendorCreditNote tương ứng
/// để credit note không còn treo với RemainingAmount > 0 trên BalanceSheet.
/// </summary>
public sealed class VendorRefundCompletedEventHandler(
    IVendorRefundManager vendorRefundManager,
    IVendorDebtManager vendorDebtManager) : INotificationHandler<VendorRefundCompleted>
{
    public async Task Handle(VendorRefundCompleted notification, CancellationToken cancellationToken)
    {
        var refund = await vendorRefundManager.GetByIdAsync(notification.VendorRefundId)
            .ConfigureAwait(false);
        if (refund is null) return;

        await vendorDebtManager.ConsumeCreditNoteByRefundAsync(
            refund.VendorReturnId,
            notification.Amount).ConfigureAwait(false);
    }
}

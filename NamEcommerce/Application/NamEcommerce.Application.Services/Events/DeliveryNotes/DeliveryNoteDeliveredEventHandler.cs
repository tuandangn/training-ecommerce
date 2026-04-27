using MediatR;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

/// <summary>
/// Khi phiếu giao hàng đã giao thành công — sinh công nợ khách hàng tương ứng (idempotent qua <c>DeliveryNoteId</c>).
/// </summary>
public sealed class DeliveryNoteDeliveredEventHandler(
    ICustomerDebtManager debtManager,
    IDeliveryNoteManager deliveryNoteManager) : INotificationHandler<DeliveryNoteDelivered>
{
    private readonly ICustomerDebtManager _debtManager = debtManager;
    private readonly IDeliveryNoteManager _deliveryNoteManager = deliveryNoteManager;

    public async Task Handle(DeliveryNoteDelivered notification, CancellationToken cancellationToken)
    {
        // Event đã carry đủ thông tin — vẫn fetch lại để lấy CreatedByUserId (audit) và đảm bảo phiếu vẫn ở trạng thái Delivered.
        var deliveryNote = await _deliveryNoteManager.GetByIdAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote == null) return;

        var createDebtDto = new CreateCustomerDebtDto
        {
            CustomerId = notification.CustomerId,
            DeliveryNoteId = notification.DeliveryNoteId,
            TotalAmount = notification.TotalAmount, // "phiếu đã xuất thì phải thu đủ"
            DueDateUtc = null,
            CreatedByUserId = deliveryNote.CreatedByUserId
        };

        await _debtManager.CreateDebtFromDeliveryNoteAsync(createDebtDto).ConfigureAwait(false);
    }
}

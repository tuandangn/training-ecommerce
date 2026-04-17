using MediatR;
using NamEcommerce.Application.Contracts.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

public sealed class DeliveryNoteDeliveredEventHandler(
    ICustomerDebtManager debtManager,
    IDeliveryNoteManager deliveryNoteManager) : INotificationHandler<DeliveryNoteDeliveredNotification>
{
    private readonly ICustomerDebtManager _debtManager = debtManager;
    private readonly IDeliveryNoteManager _deliveryNoteManager = deliveryNoteManager;

    public async Task Handle(DeliveryNoteDeliveredNotification notification, CancellationToken cancellationToken)
    {
        var deliveryNote = await _deliveryNoteManager.GetByIdAsync(notification.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote == null) return;

        var createDebtDto = new CreateCustomerDebtDto
        {
            CustomerId = deliveryNote.CustomerId,
            DeliveryNoteId = deliveryNote.Id,
            TotalAmount = deliveryNote.TotalAmount, // Based on "phiếu đã xuất thì phải thu đủ"
            DueDateUtc = null, // Set to null as per user requirement to handle later
            CreatedByUserId = deliveryNote.CreatedByUserId
        };

        await _debtManager.CreateDebtFromDeliveryNoteAsync(createDebtDto).ConfigureAwait(false);
    }
}

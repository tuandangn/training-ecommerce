using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Application.Services.Events.DeliveryNotes;

/// <summary>
/// Khi phiếu giao hàng đã giao thành công — sinh công nợ khách hàng tương ứng (idempotent qua <c>DeliveryNoteId</c>).
/// </summary>
public sealed class DeliveryNoteDeliveredEventHandler(
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

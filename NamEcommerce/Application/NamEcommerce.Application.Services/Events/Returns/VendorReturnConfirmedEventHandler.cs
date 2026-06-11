using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Events.Returns;
using NamEcommerce.Domain.Shared.Services.Returns;

namespace NamEcommerce.Application.Services.Events.Returns;

/// <summary>
/// Xử lý sự kiện <see cref="VendorReturnConfirmed"/> — phiếu trả hàng NCC vừa được Confirm.
/// <list type="number">
///   <item><description>Tạo <c>DeliveryNote(SourceType=ToVendorReturn)</c> ở trạng thái <c>Delivered</c>
///     ngay — trừ tồn thực sự, bỏ qua flow Reserve/Confirm/Delivering; không sinh CustomerDebt.</description></item>
///   <item><description>Set <c>VendorReturn.GeneratedDeliveryNoteId</c> và giảm <c>VendorDebt</c>
///     liên quan theo FIFO <c>CreatedOnUtc</c> thông qua <see cref="IVendorReturnManager.FinalizeConfirmAsync"/>.</description></item>
/// </list>
/// Idempotency: nếu <c>GeneratedDeliveryNoteId</c> đã được set, <c>FinalizeConfirmAsync</c> sẽ return sớm.
/// </summary>
public sealed class VendorReturnConfirmedEventHandler(
    IVendorReturnManager vendorReturnManager,
    IDeliveryNoteAppService deliveryNoteAppService) : INotificationHandler<VendorReturnConfirmed>
{
    private readonly IVendorReturnManager _vendorReturnManager = vendorReturnManager;
    private readonly IDeliveryNoteAppService _deliveryNoteAppService = deliveryNoteAppService;

    public async Task Handle(VendorReturnConfirmed notification, CancellationToken cancellationToken)
    {
        // Re-fetch để lấy Items đầy đủ — event chỉ mang Id để tránh reference leak qua event boundary.
        var vendorReturn = await _vendorReturnManager.GetByIdAsync(notification.VendorReturnId)
            .ConfigureAwait(false);
        if (vendorReturn is null) return;

        // Idempotency guard: đã xử lý rồi thì bỏ qua
        if (vendorReturn.GeneratedDeliveryNoteId.HasValue) return;

        // 1. Tạo DeliveryNote xuất kho (SourceType=ToVendorReturn, Status=Delivered, trừ tồn inline)
        var result = await _deliveryNoteAppService.CreateAsDeliveredFromVendorReturnAsync(
            new CreateDeliveryNoteFromVendorReturnAppDto
            {
                VendorReturnId = notification.VendorReturnId,
                WarehouseId = notification.WarehouseId,
                Items = vendorReturn.Items.Select(i => new CreateDeliveryNoteFromVendorReturnItemAppDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.AcceptedQuantity,
                    UnitCost = i.ReturnUnitCost
                })
            }).ConfigureAwait(false);

        if (!result.Success)
            return;

        // 2. Ghi nhận DeliveryNoteId + giảm VendorDebt FIFO (net = Σ AcceptedTotal - AdditionalCost)
        var totalReturnAmount = vendorReturn.NetRecoveryAmount;
        await _vendorReturnManager.FinalizeConfirmAsync(
            notification.VendorReturnId, result.CreatedId!.Value, totalReturnAmount).ConfigureAwait(false);
    }
}

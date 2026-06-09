using MediatR;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Events.Returns;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.Returns;

namespace NamEcommerce.Application.Services.Events.Returns;

/// <summary>
/// Xử lý sự kiện <see cref="VendorReturnOverRecovered"/> — tổng giá trị trả hàng NCC vượt quá
/// tổng công nợ còn lại, NCC phải hoàn tiền mặt về cho shop.
/// Tạo <c>VendorRefund</c> với <c>Amount = OverAmount</c> và trạng thái <c>Pending</c>.
/// </summary>
public sealed class VendorReturnOverRecoveredEventHandler(
    IVendorRefundManager vendorRefundManager,
    IVendorReturnManager vendorReturnManager) : INotificationHandler<VendorReturnOverRecovered>
{
    public async Task Handle(VendorReturnOverRecovered notification, CancellationToken cancellationToken)
    {
        var vendorReturn = await vendorReturnManager.GetByIdAsync(notification.VendorReturnId)
            .ConfigureAwait(false);
        if (vendorReturn is null) return;

        await vendorRefundManager.CreateAsync(new CreateVendorRefundDto
        {
            VendorId = notification.VendorId,
            VendorName = vendorReturn.VendorName,
            VendorReturnId = notification.VendorReturnId,
            VendorReturnCode = vendorReturn.Code,
            Amount = notification.OverAmount
        }).ConfigureAwait(false);
    }
}

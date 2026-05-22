using MediatR;
using NamEcommerce.Domain.Shared.Events.Returns;

namespace NamEcommerce.Application.Services.Events.Returns;

/// <summary>
/// Audit hook cho <see cref="VendorReturnCancelled"/>. Việc hủy phiếu trả NCC chỉ xảy ra ở
/// Draft/Inspecting nên không có side-effect kho/công nợ cần hoàn nguyên. Handler hiện chỉ là
/// điểm tích hợp cho audit log/notification tương lai; không xóa event vì các consumer ngoài
/// (notification service, reporting) cần subscribe.
/// </summary>
public sealed class VendorReturnCancelledEventHandler : INotificationHandler<VendorReturnCancelled>
{
    public Task Handle(VendorReturnCancelled notification, CancellationToken cancellationToken)
    {
        // Future: emit audit entry / notify watchers. Hiện tại NoOp.
        return Task.CompletedTask;
    }
}

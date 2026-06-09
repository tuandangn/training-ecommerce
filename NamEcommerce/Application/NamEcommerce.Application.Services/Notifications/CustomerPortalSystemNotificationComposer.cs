using NamEcommerce.Application.Contracts.Dtos.Notifications;
using NamEcommerce.Domain.Shared.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.Notifications;

namespace NamEcommerce.Application.Services.Notifications;

public static class CustomerPortalSystemNotificationComposer
{
    private const string OrderRequestRelatedEntityType = "CustomerOrderRequest";
    private const string ReturnRequestRelatedEntityType = "CustomerReturnRequest";
    private const string DeliveryNoteRelatedEntityType = "DeliveryNote";
    private const string OrdersViewPermission = "Orders.View";
    private const string CustomerReturnsManagePermission = "CustomerReturns.Manage";
    private const string DeliveryNotesViewPermission = "DeliveryNotes.View";

    public static CreateSystemNotificationAppDto OrderRequestCreated(CustomerOrderRequestDto request)
        => new()
        {
            Type = (int)SystemNotificationType.CustomerPortalOrderRequestCreated,
            Severity = (int)SystemNotificationSeverity.Warning,
            Title = $"Yêu cầu đặt hàng mới {request.Code}",
            Message = "Khách vừa tạo yêu cầu đặt hàng trên Customer Portal. Vui lòng kiểm tra hàng hóa, định giá và duyệt nếu hợp lệ.",
            RequiredPermission = OrdersViewPermission,
            RelatedEntityId = request.Id,
            RelatedEntityType = OrderRequestRelatedEntityType,
            ActionUrl = $"/CustomerPortal/OrderRequestDetails/{request.Id}"
        };

    public static CreateSystemNotificationAppDto ReturnRequestCreated(CustomerReturnRequestDto request, string? deliveryNoteCode)
        => new()
        {
            Type = (int)SystemNotificationType.CustomerPortalReturnRequestCreated,
            Severity = (int)SystemNotificationSeverity.Warning,
            Title = string.IsNullOrWhiteSpace(deliveryNoteCode)
                ? "Yêu cầu trả hàng mới"
                : $"Yêu cầu trả hàng cho phiếu {deliveryNoteCode}",
            Message = "Khách vừa tạo yêu cầu trả hàng trên Customer Portal. Vui lòng xem số lượng, lý do và hình ảnh hiện trạng nếu có.",
            RequiredPermission = CustomerReturnsManagePermission,
            RelatedEntityId = request.Id,
            RelatedEntityType = ReturnRequestRelatedEntityType,
            ActionUrl = $"/CustomerPortal/ReturnRequestDetails/{request.Id}"
        };

    public static CreateSystemNotificationAppDto DeliveryConfirmed(Guid deliveryNoteId, string deliveryNoteCode)
        => new()
        {
            Type = (int)SystemNotificationType.CustomerPortalDeliveryConfirmed,
            Severity = (int)SystemNotificationSeverity.Info,
            Title = $"Khách đã nhận hàng phiếu {deliveryNoteCode}",
            Message = "Khách vừa xác nhận đã nhận hàng trên Customer Portal.",
            RequiredPermission = DeliveryNotesViewPermission,
            RelatedEntityId = deliveryNoteId,
            RelatedEntityType = DeliveryNoteRelatedEntityType,
            ActionUrl = $"/DeliveryNote/Details/{deliveryNoteId}"
        };
}

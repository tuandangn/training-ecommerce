using NamEcommerce.Application.Contracts.Dtos.Notifications;
using NamEcommerce.Domain.Shared.Enums.Notifications;

namespace NamEcommerce.Application.Services.Notifications;

public static class DeliveryNoteSystemNotificationComposer
{
    private const string ManageDeliveryNotesPermission = "DeliveryNotes.Manage";

    public static CreateSystemNotificationAppDto ShipperNotResponded(Guid deliveryNoteId, string deliveryNoteCode, string? shipperName)
        => new()
        {
            Type = (int)SystemNotificationType.DeliveryNoteShipperNotResponded,
            Severity = (int)SystemNotificationSeverity.Info,
            Title = "Phiếu xuất được xử lý thay người giao",
            Message = $"Phiếu xuất {deliveryNoteCode} đã được hoàn thành bởi admin/kho trong khi đang được giao bởi {shipperName ?? "người giao hàng"}.",
            RequiredPermission = ManageDeliveryNotesPermission,
            RelatedEntityId = deliveryNoteId,
            RelatedEntityType = "DeliveryNote",
            ActionUrl = $"/DeliveryNotes/Detail?id={deliveryNoteId}"
        };
}

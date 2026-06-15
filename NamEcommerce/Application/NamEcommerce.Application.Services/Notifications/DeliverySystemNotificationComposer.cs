using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Notifications;
using NamEcommerce.Domain.Shared.Enums.Notifications;

namespace NamEcommerce.Application.Services.Notifications;

public static class DeliverySystemNotificationComposer
{
    private const string DeliveryNoteRelatedEntityType = "DeliveryNote";
    private const string DeliveryRunRelatedEntityType = "DeliveryRun";
    private const string DeliveryNotesManagePermission = "DeliveryNotes.Manage";
    private const string DeliveryRunsViewPermission = "DeliveryRuns.View";
    private const string DeliveryRunsManagePermission = "DeliveryRuns.Manage";
    private const string DeliveryRunsConfirmCashHandoverPermission = "DeliveryRuns.ConfirmCashHandover";

    public static CreateSystemNotificationAppDto DeliveryNoteCreated(DeliveryNoteAppDto note)
        => DeliveryNoteNotification(
            note,
            SystemNotificationType.DeliveryNoteCreated,
            SystemNotificationSeverity.Warning,
            $"Phiếu xuất mới {note.Code}",
            BuildDeliveryNoteMessage(note, "Phiếu xuất mới vừa được tạo và cần xử lý."),
            DeliveryNotesManagePermission);

    public static CreateSystemNotificationAppDto DeliveryNoteConfirmed(DeliveryNoteAppDto note)
        => DeliveryNoteNotification(
            note,
            SystemNotificationType.DeliveryNoteConfirmed,
            SystemNotificationSeverity.Warning,
            $"Phiếu xuất {note.Code} đã xác nhận",
            BuildDeliveryNoteMessage(note, "Phiếu xuất đã xác nhận và cần đưa vào chuyến giao."),
            DeliveryRunsManagePermission);

    public static CreateSystemNotificationAppDto DeliveryNoteDelivering(DeliveryNoteAppDto note)
        => DeliveryNoteNotification(
            note,
            SystemNotificationType.DeliveryNoteDelivering,
            SystemNotificationSeverity.Info,
            $"Đang giao phiếu {note.Code}",
            BuildDeliveryNoteMessage(note, "Người giao hàng vừa bắt đầu giao phiếu xuất."),
            DeliveryRunsViewPermission);

    public static CreateSystemNotificationAppDto DeliveryNoteDelivered(DeliveryNoteAppDto note)
        => DeliveryNoteNotification(
            note,
            SystemNotificationType.DeliveryNoteDelivered,
            SystemNotificationSeverity.Success,
            $"Đã giao phiếu {note.Code}",
            BuildDeliveryNoteMessage(note, "Người giao hàng vừa cập nhật giao hàng hoàn tất."),
            DeliveryRunsViewPermission);

    public static CreateSystemNotificationAppDto DeliveryRunCreated(DeliveryRunAppDto run)
        => DeliveryRunNotification(
            run,
            SystemNotificationType.DeliveryRunCreated,
            SystemNotificationSeverity.Warning,
            $"Chuyến giao mới {run.Code}",
            $"Chuyến giao mới được tạo cho {run.AssignedDeliveryFullName} với {run.Items.Count} phiếu xuất.",
            DeliveryRunsManagePermission);

    public static CreateSystemNotificationAppDto DeliveryRunHandedOver(DeliveryRunAppDto run)
        => DeliveryRunNotification(
            run,
            SystemNotificationType.DeliveryRunHandedOver,
            SystemNotificationSeverity.Info,
            $"Đã bàn giao chuyến {run.Code}",
            $"Chuyến giao đã bàn giao cho {run.AssignedDeliveryFullName}.",
            DeliveryRunsViewPermission);

    public static CreateSystemNotificationAppDto DeliveryRunCashHandoverPending(DeliveryRunAppDto run)
        => DeliveryRunNotification(
            run,
            SystemNotificationType.DeliveryRunCashHandoverPending,
            SystemNotificationSeverity.Warning,
            $"Chuyến {run.Code} cần xác nhận tiền",
            "Chuyến giao có phiếu COD, cần theo dõi tiền shipper đã thu sau khi giao hàng.",
            DeliveryRunsConfirmCashHandoverPermission);

    private static CreateSystemNotificationAppDto DeliveryNoteNotification(
        DeliveryNoteAppDto note,
        SystemNotificationType type,
        SystemNotificationSeverity severity,
        string title,
        string message,
        string requiredPermission)
        => new()
        {
            Type = (int)type,
            Severity = (int)severity,
            Title = title,
            Message = message,
            RequiredPermission = requiredPermission,
            RelatedEntityId = note.Id,
            RelatedEntityType = DeliveryNoteRelatedEntityType,
            ActionUrl = $"/DeliveryNote/Details/{note.Id}"
        };

    private static CreateSystemNotificationAppDto DeliveryRunNotification(
        DeliveryRunAppDto run,
        SystemNotificationType type,
        SystemNotificationSeverity severity,
        string title,
        string message,
        string requiredPermission)
        => new()
        {
            Type = (int)type,
            Severity = (int)severity,
            Title = title,
            Message = message,
            RequiredPermission = requiredPermission,
            RelatedEntityId = run.Id,
            RelatedEntityType = DeliveryRunRelatedEntityType,
            ActionUrl = $"/DeliveryRun/Details/{run.Id}"
        };

    private static string BuildDeliveryNoteMessage(DeliveryNoteAppDto note, string prefix)
        => string.IsNullOrWhiteSpace(note.OrderCode)
            ? $"{prefix} Khách hàng: {note.CustomerName}."
            : $"{prefix} Đơn hàng: {note.OrderCode}. Khách hàng: {note.CustomerName}.";
}

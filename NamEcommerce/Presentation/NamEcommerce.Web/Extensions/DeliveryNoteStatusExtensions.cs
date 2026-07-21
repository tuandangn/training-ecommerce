using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

namespace NamEcommerce.Web.Extensions;

public static class DeliveryNoteStatusExtensions
{
    extension(DeliveryNoteStatus status)
    {
        public string GetDisplayText() => status switch
        {
            DeliveryNoteStatus.Draft => "Bản nháp",
            DeliveryNoteStatus.Confirmed => "Đã xác nhận",
            DeliveryNoteStatus.Delivering => "Đang giao",
            DeliveryNoteStatus.PendingConfirmation => "Đang chờ xác nhận",
            DeliveryNoteStatus.Delivered => "Đã giao",
            DeliveryNoteStatus.Cancelled => "Đã hủy",
            _ => throw new InvalidDataException(nameof(status)),
        };

        public string GetDisplayColor() => status switch
        {
            DeliveryNoteStatus.Draft => "bg-secondary text-light",
            DeliveryNoteStatus.Confirmed => "bg-info text-light",
            DeliveryNoteStatus.Delivering => "bg-warning text-light",
            DeliveryNoteStatus.PendingConfirmation => "bg-warning text-light",
            DeliveryNoteStatus.Delivered => "bg-success text-light",
            DeliveryNoteStatus.Cancelled => "bg-danger text-light",
            _ => throw new InvalidDataException(nameof(status)),
        };

        public string GetActionIcon() => status switch
        {
            DeliveryNoteStatus.Draft => "bi-pencil",
            DeliveryNoteStatus.Confirmed => "bi-check-all",
            DeliveryNoteStatus.Delivering => "bi-truck",
            DeliveryNoteStatus.PendingConfirmation => "bi-hourglass-split",
            DeliveryNoteStatus.Delivered => "bi-check-circle-fill",
            DeliveryNoteStatus.Cancelled => "bi-x-circle",
            _ => throw new InvalidDataException(nameof(status)),
        };
    }
}

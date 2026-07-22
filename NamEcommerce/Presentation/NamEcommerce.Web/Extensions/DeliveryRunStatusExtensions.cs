using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

namespace NamEcommerce.Web.Extensions;

public static class DeliveryRunStatusExtensions
{
    extension(DeliveryRunStatus status)
    {
        public string GetDisplayColor() => status switch
        {
            DeliveryRunStatus.Planning => "bg-secondary text-white",
            DeliveryRunStatus.ReadyForHandover => "bg-info text-white",
            DeliveryRunStatus.HandedToDriver => "bg-warning text-white",
            DeliveryRunStatus.Closed => "bg-success text-white",
            _ => "bg-secondary text-white"
        };

        public string GetDisplayName() => status switch
        {
            DeliveryRunStatus.Planning => "Đang lập",
            DeliveryRunStatus.ReadyForHandover => "Chờ bàn giao",
            DeliveryRunStatus.HandedToDriver => "Đã bàn giao",
            DeliveryRunStatus.Closed => "Đã đóng",
            DeliveryRunStatus.Cancelled => "Đã hủy",
            _ => "Không xác định"
        };
    }
}

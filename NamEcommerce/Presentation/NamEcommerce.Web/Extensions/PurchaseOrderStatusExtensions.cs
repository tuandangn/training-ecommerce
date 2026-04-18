using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Web.Extensions;

public static class PurchaseOrderStatusExtensions
{
    extension(PurchaseOrderStatus)
    {
        public static IEnumerable<(int value, string text)> GetOptions()
            => Enum.GetValues<PurchaseOrderStatus>().Select(status => ((int)status, status.GetDisplayText()));
    }

    extension(PurchaseOrderStatus status)
    {
        public string GetDisplayText() => status switch
        {
            PurchaseOrderStatus.Draft => "Bản nháp",
            PurchaseOrderStatus.Submitted => "Đã gửi",
            PurchaseOrderStatus.Approved => "Đã duyệt",
            PurchaseOrderStatus.Receiving => "Đang nhận",
            PurchaseOrderStatus.Completed => "Hoàn tất",
            PurchaseOrderStatus.Cancelled => "Đã hủy",
            _ => throw new InvalidDataException(nameof(status)),
        };

        public string GetDisplayColor() => status switch
        {
            PurchaseOrderStatus.Draft => "secondary",
            PurchaseOrderStatus.Submitted => "primary",
            PurchaseOrderStatus.Approved => "info",
            PurchaseOrderStatus.Receiving => "warning text-dark",
            PurchaseOrderStatus.Completed => "success",
            PurchaseOrderStatus.Cancelled => "danger",
            _ => throw new InvalidDataException(nameof(status)),
        };
    }
}

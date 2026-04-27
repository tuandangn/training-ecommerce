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
            PurchaseOrderStatus.Draft => "bg-secondary text-light",
            PurchaseOrderStatus.Submitted => "bg-primary text-light",
            PurchaseOrderStatus.Approved => "bg-info text-light",
            PurchaseOrderStatus.Receiving => "bg-warning text-dark",
            PurchaseOrderStatus.Completed => "bg-success text-light",
            PurchaseOrderStatus.Cancelled => "bg-danger text-light",
            _ => throw new InvalidDataException(nameof(status)),
        };
    }
}

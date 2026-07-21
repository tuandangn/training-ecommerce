using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Web.Extensions;

public static class AllocationStatusExtensions
{

    extension(AllocationStatus status)
    {
        public string GetDisplayText() => status switch
        {
            AllocationStatus.Allocated => "Đã phân bổ",
            AllocationStatus.PartiallyReceived => "Nhận một phần",
            AllocationStatus.FullyReceived => "Đã nhận đủ",
            AllocationStatus.DeliveryPending => "Chờ khách xác nhận",
            AllocationStatus.DeliveryConfirmed => "Khách đã nhận",
            AllocationStatus.Cancelled => "Đã hủy",
            _ => throw new InvalidDataException(nameof(status)),
        };

        public string GetDisplayColor() => status switch
        {
            AllocationStatus.Allocated => "secondary",
            AllocationStatus.PartiallyReceived => "warning",
            AllocationStatus.FullyReceived => "success",
            AllocationStatus.DeliveryPending => "info text-white",
            AllocationStatus.DeliveryConfirmed => "success",
            AllocationStatus.Cancelled => "danger",
            _ => throw new InvalidDataException(nameof(status)),
        };
    }
}

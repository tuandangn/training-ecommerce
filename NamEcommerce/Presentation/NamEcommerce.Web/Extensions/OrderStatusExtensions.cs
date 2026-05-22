using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Web.Extensions;

public static class OrderStatusExtensions
{
    extension(OrderStatus)
    {
        public static IEnumerable<(int value, string text)> GetOptions()
            => Enum.GetValues<OrderStatus>().Select(status => ((int)status, status.GetDisplayText()));
    }

    extension(OrderStatus status)
    {
        public string GetDisplayText() => status switch
        {
            OrderStatus.Pending => "Đang xử lý",
            OrderStatus.Completed => "Hoàn thành",
            OrderStatus.Cancelled => "Đã hủy",
            _ => throw new InvalidDataException(nameof(status)),
        };

        public string GetDisplayColor() => status switch
        {
            OrderStatus.Pending => "bg-info text-light",
            OrderStatus.Completed => "bg-success text-light",
            OrderStatus.Cancelled => "bg-danger text-light",
            _ => throw new InvalidDataException(nameof(status)),
        };
    }
}

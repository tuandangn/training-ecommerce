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
            OrderStatus.Pending => "Đang chờ",
            OrderStatus.Locked => "Đã khóa",
            _ => throw new InvalidDataException(nameof(status)),
        };

        public string GetDisplayColor() => status switch
        {
            OrderStatus.Pending => "bg-secondary text-light",
            OrderStatus.Locked => "bg-danger text-light",
            _ => throw new InvalidDataException(nameof(status)),
        };
    }
}

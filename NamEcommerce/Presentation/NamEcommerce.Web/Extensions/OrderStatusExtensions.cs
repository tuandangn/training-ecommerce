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
            OrderStatus.Pending => "Chưa khóa",
            OrderStatus.Locked => "Đã khóa",
            _ => throw new InvalidDataException(nameof(status)),
        };

        public string GetDisplayColor() => status switch
        {
            OrderStatus.Pending => "bg-light text-dark",
            OrderStatus.Locked => "bg-success text-light",
            _ => throw new InvalidDataException(nameof(status)),
        };
    }
}

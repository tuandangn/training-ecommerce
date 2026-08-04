using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Web.Extensions;

public static class PaymentMethodExtensions
{
    extension(PaymentMethod)
    {
        public static IEnumerable<(int value, string text)> GetOptions()
            => Enum.GetValues<PaymentMethod>().Select(status => ((int)status, status.GetDisplayText()));
    }

    extension(PaymentMethod method)
    {
        public string GetDisplayText() => method switch
        {
            PaymentMethod.Cash => "Tiền mặt",
            PaymentMethod.BankTransfer => "Chuyển khoản",
            PaymentMethod.COD => "Thanh toán khi nhận hàng",
            PaymentMethod.Other => "Khác",
            _ => method.ToString(),
        };

        public string GetDisplayColor() => method switch
        {
            PaymentMethod.Cash => "bg-success text-light",
            PaymentMethod.BankTransfer => "bg-light",
            PaymentMethod.COD => "bg-info text-light",
            _ => "bg-secondary text-light",
        };
    }
}

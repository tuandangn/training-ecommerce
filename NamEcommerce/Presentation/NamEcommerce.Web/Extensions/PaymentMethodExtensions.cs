using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Web.Extensions;

public static class PaymentMethodExtensions
{
    public static string GetDisplayText(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.BankTransfer => "Chuyển khoản",
        PaymentMethod.COD => "Thanh toán khi nhận hàng",
        PaymentMethod.Other => "Khác",
        _ => method.ToString(),
    };

    public static string GetDisplayColor(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "bg-success text-light",
        PaymentMethod.BankTransfer => "bg-light",
        PaymentMethod.COD => "bg-info text-light",
        _ => "bg-secondary text-light",
    };
}

using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Web.Extensions;

public static class PaymentTypeExtensions
{
    public static string GetDisplayText(this PaymentType type) => type switch
    {
        PaymentType.DebtPayment => "Trả nợ",
        PaymentType.Deposit => "Tiền cọc",
        PaymentType.General => "Chung",
        _ => type.ToString(),
    };

    public static string GetDisplayColor(this PaymentType type) => type switch
    {
        PaymentType.DebtPayment => "bg-success text-light",
        PaymentType.Deposit => "bg-info text-light",
        PaymentType.General => "bg-light",
        _ => "bg-secondary text-light",
    };
}

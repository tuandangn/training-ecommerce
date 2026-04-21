using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Web.Extensions;

public static class PaymentTypeExtensions
{
    public static string GetDisplayText(this PaymentType type) => type switch
    {
        PaymentType.DebtPayment => "Trả nợ KH",
        PaymentType.Deposit => "Tiền cọc",
        PaymentType.General => "Chung",
        PaymentType.AdvancePayment => "Ứng trước NCC",
        PaymentType.VendorDebtPayment => "Trả nợ NCC",
        _ => type.ToString(),
    };

    public static string GetDisplayColor(this PaymentType type) => type switch
    {
        PaymentType.DebtPayment => "bg-success text-light",
        PaymentType.Deposit => "bg-info text-light",
        PaymentType.General => "bg-light",
        PaymentType.AdvancePayment => "bg-warning text-dark",
        PaymentType.VendorDebtPayment => "bg-danger text-light",
        _ => "bg-secondary text-light",
    };
}

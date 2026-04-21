using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Web.Extensions;

public static class DebtStatusExtensions
{
    public static string GetDisplayText(this DebtStatus status) => status switch
    {
        DebtStatus.Outstanding => "Chưa thanh toán",
        DebtStatus.PartiallyPaid => "Thanh toán một phần",
        DebtStatus.FullyPaid => "Đã thanh toán",
        _ => status.ToString(),
    };

    // Dùng cho int (sau khi map qua AppDto / ViewModel)
    public static string ToVietnamese(this DebtStatus status) => status.GetDisplayText();

    public static string GetDisplayColor(this DebtStatus status) => status switch
    {
        DebtStatus.Outstanding => "bg-danger text-light",
        DebtStatus.PartiallyPaid => "bg-warning text-dark",
        DebtStatus.FullyPaid => "bg-success text-light",
        _ => "bg-secondary text-light",
    };

    public static string ToVietnamese(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.BankTransfer => "Chuyển khoản",
        _ => method.ToString(),
    };

    public static string ToVietnamese(this PaymentType type) => type switch
    {
        PaymentType.DebtPayment => "Trả nợ",
        PaymentType.Deposit => "Tiền cọc",
        PaymentType.General => "Thanh toán chung",
        _ => type.ToString(),
    };
}

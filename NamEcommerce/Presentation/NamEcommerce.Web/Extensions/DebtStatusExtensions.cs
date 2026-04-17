using NamEcommerce.Domain.Shared.Enums.Debts;

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

    public static string GetDisplayColor(this DebtStatus status) => status switch
    {
        DebtStatus.Outstanding => "bg-danger",
        DebtStatus.PartiallyPaid => "bg-warning text-dark",
        DebtStatus.FullyPaid => "bg-success",
        _ => "bg-secondary",
    };
}

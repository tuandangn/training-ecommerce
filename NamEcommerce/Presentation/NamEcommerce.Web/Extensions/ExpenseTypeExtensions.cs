using NamEcommerce.Domain.Shared.Enums.Finance;

namespace NamEcommerce.Web.Extensions;

public static class ExpenseTypeExtensions
{

    extension(ExpenseType)
    {
        public static IEnumerable<(int value, string text)> GetOptions()
            => Enum.GetValues<ExpenseType>().Select(status => ((int)status, status.GetDisplayText()));
    }

    extension(ExpenseType type)
    {
        public string GetDisplayText() => type switch
        {
            ExpenseType.Sale => "Bán hàng",
            ExpenseType.Management => "Quản lý",
            ExpenseType.Payroll => "Lương thưởng",
            ExpenseType.Rent => "Mặt bằng",
            ExpenseType.Marketing => "Tiếp thị",
            ExpenseType.Utilities => "Điện nước",
            ExpenseType.General => "Khác",
            ExpenseType.ReturnCost => "Hoàn hàng",
            ExpenseType.AssetDisposal => "Khấu hao",
            _ => throw new InvalidDataException(nameof(type)),
        };

        public string GetDisplayColor() => type switch
        {
            ExpenseType.Sale => "bg-primary text-light",
            ExpenseType.Management => "bg-primary text-light",
            ExpenseType.Payroll => "bg-info text-light",
            ExpenseType.Rent => "bg-warning text-light",
            ExpenseType.Marketing => "bg-primary text-light",
            ExpenseType.Utilities => "bg-secondary text-light",
            ExpenseType.General => "bg-light",
            ExpenseType.ReturnCost => "bg-danger text-light",
            ExpenseType.AssetDisposal => "bg-secondary text-light",
            _ => throw new InvalidDataException(nameof(type)),
        };
    }
}

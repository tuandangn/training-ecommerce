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
            ExpenseType.Payroll => "Lương thưởng",
            ExpenseType.Rent => "Mặt bằng",
            ExpenseType.Marketing => "Tiếp thị",
            ExpenseType.Utilities => "Điện nước",
            ExpenseType.General => "Khác",
            ExpenseType.ReturnCost => "Hoàn hàng",
            ExpenseType.AssetDisposal => "Khấu hao",
            _ => throw new InvalidDataException(nameof(type)),
        };
    }
}

namespace NamEcommerce.Domain.Shared.Enums.Finance;

public enum ExpenseType
{
    Payroll = 1,
    Rent = 2,
    Marketing = 3,
    Utilities = 4,
    General = 5,

    /// <summary>Chi phí phát sinh khi nhận hàng trả / trả hàng NCC (vận chuyển, bồi thường hư hỏng...).</summary>
    ReturnCost = 6,
    AssetDisposal = 7
}

using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Finance;

[Serializable]
public sealed class UpsertExpenseBudgetCommand : ICommand<CommonActionResultModel>
{
    public required int ExpenseType { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required decimal Amount { get; init; }
}

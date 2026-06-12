using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Finance;

public sealed class CreateExpenseCommand : ICommand<CommonActionResultModel>
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required decimal Amount { get; init; }
    public required int ExpenseType { get; init; }
    public required DateTime IncurredDate { get; init; }
    public Guid? SourceOrderId { get; init; }
}

public sealed class UpdateExpenseCommand : ICommand<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required decimal Amount { get; init; }
    public required int ExpenseType { get; init; }
    public required DateTime IncurredDate { get; init; }
}

public sealed record DeleteExpenseCommand(Guid Id) : ICommand<CommonActionResultModel>;

public sealed class UpsertExpenseBudgetCommand : ICommand<CommonActionResultModel>
{
    public required int ExpenseType { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required decimal Amount { get; init; }
}

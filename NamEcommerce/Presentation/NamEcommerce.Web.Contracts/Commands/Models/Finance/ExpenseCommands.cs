using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Finance;

public sealed class CreateExpenseCommand : IRequest<CommonActionResultModel>
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required decimal Amount { get; init; }
    public required int ExpenseType { get; init; }
    public required DateTime IncurredDate { get; init; }
}

public sealed class UpdateExpenseCommand : IRequest<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required decimal Amount { get; init; }
    public required int ExpenseType { get; init; }
    public required DateTime IncurredDate { get; init; }
}

public sealed record DeleteExpenseCommand(Guid Id) : IRequest<CommonActionResultModel>;

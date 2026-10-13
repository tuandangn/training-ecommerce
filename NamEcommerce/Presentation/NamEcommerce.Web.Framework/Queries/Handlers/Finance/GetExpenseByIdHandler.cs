using MediatR;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Web.Contracts.Models.Finance;
using NamEcommerce.Web.Contracts.Queries.Models.Finance;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Finance;

public sealed class GetExpenseByIdHandler(IExpenseAppService expenseAppService)
    : IRequestHandler<GetExpenseByIdQuery, ExpenseModel?>
{
    public async Task<ExpenseModel?> Handle(GetExpenseByIdQuery request, CancellationToken cancellationToken)
    {
        var expense = await expenseAppService.GetExpenseByIdAsync(request.Id).ConfigureAwait(false);
        if (expense is null) return null;

        return new ExpenseModel
        {
            Id = expense.Id,
            Title = expense.Title,
            Description = expense.Description,
            AmountWithoutTax = expense.AmountWithoutTax,
            TaxRate = expense.TaxRate,
            ExpenseType = expense.ExpenseType,
            IncurredDate = DateTimeHelper.ToLocalTime(expense.IncurredDateUtc),
            IsSystemGenerated = expense.IsSystemGenerated
        };
    }
}

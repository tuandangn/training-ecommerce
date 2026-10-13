using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Finance;

public sealed class UpsertExpenseBudgetHandler(IExpenseBudgetAppService budgetAppService) : IRequestHandler<UpsertExpenseBudgetCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(UpsertExpenseBudgetCommand request, CancellationToken cancellationToken)
    {
        var result = await budgetAppService.UpsertBudgetAsync(new UpsertExpenseBudgetAppDto
        {
            ExpenseType = request.ExpenseType,
            Year = request.Year,
            Month = request.Month,
            Amount = request.Amount
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}

using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Finance;

public sealed class UpdateExpenseHandler(IExpenseAppService expenseAppService) : IRequestHandler<UpdateExpenseCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        var result = await expenseAppService.UpdateExpenseAsync(new UpdateExpenseAppDto
        {
            Id = request.Id,
            Title = request.Title,
            Description = request.Description,
            AmountWithoutTax = request.AmountWithoutTax,
            IncurredDateUtc = DateTimeHelper.ToUniversalTime(request.IncurredDate),
            ExpenseType = request.ExpenseType,
            TaxRate = request.TaxRate
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

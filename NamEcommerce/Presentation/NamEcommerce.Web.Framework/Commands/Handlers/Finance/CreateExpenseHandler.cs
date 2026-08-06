using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Finance;

public sealed class CreateExpenseHandler(IExpenseAppService expenseAppService, ICurrentUserService currentUserService) : IRequestHandler<CreateExpenseCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var result = await expenseAppService.CreateExpenseAsync(new CreateExpenseAppDto
        {
            Title = request.Title,
            Description = request.Description,
            AmountWithoutTax = request.AmountWithoutTax,
            ExpenseType = request.ExpenseType,
            TaxRate = request.TaxRate,
            IncurredDateUtc = DateTimeHelper.ToUniversalTime(request.IncurredDate),
            OrderId = request.OrderId,
            RecordedByUserId = currentUser?.Id
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

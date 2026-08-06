using MediatR;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Finance;

public sealed class DeleteExpenseHandler(IExpenseAppService expenseAppService) : IRequestHandler<DeleteExpenseCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var result = await expenseAppService.DeleteExpenseAsync(request.Id).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}

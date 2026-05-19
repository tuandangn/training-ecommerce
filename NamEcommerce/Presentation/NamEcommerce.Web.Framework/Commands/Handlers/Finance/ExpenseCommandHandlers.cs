using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Finance;

public sealed class CreateExpenseHandler(IExpenseAppService expenseAppService)
    : IRequestHandler<CreateExpenseCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var result = await expenseAppService.CreateExpenseAsync(new CreateExpenseAppDto
        {
            Title = request.Title,
            Description = request.Description,
            Amount = request.Amount,
            ExpenseType = request.ExpenseType,
            IncurredDate = request.IncurredDate,
            SourceOrderId = request.SourceOrderId
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class UpdateExpenseHandler(IExpenseAppService expenseAppService)
    : IRequestHandler<UpdateExpenseCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        var result = await expenseAppService.UpdateExpenseAsync(new UpdateExpenseAppDto
        {
            Id = request.Id,
            Title = request.Title,
            Description = request.Description,
            Amount = request.Amount,
            ExpenseType = request.ExpenseType,
            IncurredDate = request.IncurredDate
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class DeleteExpenseHandler(IExpenseAppService expenseAppService)
    : IRequestHandler<DeleteExpenseCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var result = await expenseAppService.DeleteExpenseAsync(request.Id).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Finance;

public sealed class CreateGeneralExpenseHandlers(IExpenseAppService expenseAppService, ICurrentUserService currentUserService) : IRequestHandler<CreateGeneralExpenseCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CreateGeneralExpenseCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var result = await expenseAppService.CreateGeneralExpenseAsync(new CreateExpenseAppDto
        {
            Title = request.Title,
            Description = request.Description,
            AmountWithoutTax = request.AmountWithoutTax,
            ExpenseType = request.ExpenseType,
            TaxRate = request.TaxRate,
            IncurredDateUtc = DateTimeHelper.ToUniversalTime(request.IncurredDate),
            RecordedByUserId = currentUser?.Id
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class CreateOrderExpenseHandlers(IExpenseAppService expenseAppService, ICurrentUserService currentUserService) :
    IRequestHandler<CreateOrderExpenseCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CreateOrderExpenseCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var result = await expenseAppService.CreateOrderExpenseAsync(request.OrderId, new CreateExpenseAppDto
        {
            Title = request.Title,
            Description = request.Description,
            AmountWithoutTax = request.AmountWithoutTax,
            ExpenseType = request.ExpenseType,
            TaxRate = request.TaxRate,
            IncurredDateUtc = DateTimeHelper.ToUniversalTime(request.IncurredDate),
            RecordedByUserId = currentUser?.Id
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class CreatePurchaseOrderExpenseHandlers(IExpenseAppService expenseAppService, ICurrentUserService currentUserService) 
    : IRequestHandler<CreatePurchaseOrderExpenseCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CreatePurchaseOrderExpenseCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var result = await expenseAppService.CreatePurchaseOrderExpenseAsync(request.PurchaseOrderId, new CreateExpenseAppDto
        {
            Title = request.Title,
            Description = request.Description,
            AmountWithoutTax = request.AmountWithoutTax,
            ExpenseType = request.ExpenseType,
            TaxRate = request.TaxRate,
            IncurredDateUtc = DateTimeHelper.ToUniversalTime(request.IncurredDate),
            RecordedByUserId = currentUser?.Id
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class CreateCustomerReturnExpenseHandlers(IExpenseAppService expenseAppService, ICurrentUserService currentUserService) 
    : IRequestHandler<CreateCustomerReturnExpenseCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CreateCustomerReturnExpenseCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var result = await expenseAppService.CreateCustomerReturnExpenseAsync(request.CustomerReturnId, new CreateExpenseAppDto
        {
            Title = request.Title,
            Description = request.Description,
            AmountWithoutTax = request.AmountWithoutTax,
            ExpenseType = request.ExpenseType,
            TaxRate = request.TaxRate,
            IncurredDateUtc = DateTimeHelper.ToUniversalTime(request.IncurredDate),
            RecordedByUserId = currentUser?.Id
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class CreateVendorReturnExpenseHandlers(IExpenseAppService expenseAppService, ICurrentUserService currentUserService) 
    : IRequestHandler<CreateVendorReturnExpenseCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CreateVendorReturnExpenseCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var result = await expenseAppService.CreateVendorReturnExpenseAsync(request.VendorReturnId, new CreateExpenseAppDto
        {
            Title = request.Title,
            Description = request.Description,
            AmountWithoutTax = request.AmountWithoutTax,
            ExpenseType = request.ExpenseType,
            TaxRate = request.TaxRate,
            IncurredDateUtc = DateTimeHelper.ToUniversalTime(request.IncurredDate),
            RecordedByUserId = currentUser?.Id
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}


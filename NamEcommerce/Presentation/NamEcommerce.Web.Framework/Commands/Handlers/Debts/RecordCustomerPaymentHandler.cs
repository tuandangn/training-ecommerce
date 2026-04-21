using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class RecordCustomerPaymentHandler(
    ICustomerDebtAppService debtAppService,
    ICurrentUserService currentUserService) : IRequestHandler<RecordCustomerPaymentCommand, CommonActionResultModel>
{
    private readonly ICustomerDebtAppService _debtAppService = debtAppService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<CommonActionResultModel> Handle(RecordCustomerPaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);

            var dto = new CreateCustomerPaymentAppDto
            {
                CustomerId = request.Model.CustomerId,
                CustomerDebtId = request.Model.CustomerDebtId,
                OrderId = request.Model.OrderId,
                Amount = request.Model.Amount,
                PaymentMethod = request.Model.PaymentMethod,
                PaymentType = request.Model.PaymentType,
                Note = request.Model.Note,
                PaidOnUtc = request.Model.PaidOnUtc.ToUniversalTime(),
                RecordedByUserId = currentUser?.Id
            };

            await _debtAppService.RecordPaymentAsync(dto).ConfigureAwait(false);
            return new CommonActionResultModel { Success = true };
        }
        catch (Exception ex)
        {
            return new CommonActionResultModel { Success = false, ErrorMessage = ex.Message };
        }
    }
}

using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class RecordCustomerPaymentHandler(ICustomerDebtAppService debtAppService) : IRequestHandler<RecordCustomerPaymentCommand, CommonResultModel>
{
    private readonly ICustomerDebtAppService _debtAppService = debtAppService;

    public async Task<CommonResultModel> Handle(RecordCustomerPaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
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
                RecordedByUserId = null // This should be set from the current user in a real app
            };

            await _debtAppService.RecordPaymentAsync(dto).ConfigureAwait(false);

            return new CommonResultModel { Success = true };
        }
        catch (Exception ex)
        {
            return new CommonResultModel { Success = false, ErrorMessage = ex.Message };
        }
    }
}

using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.FastSales;
using NamEcommerce.Web.Framework.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class GetBankTransferPaymentIntentStatusHandler(IBankTransferPaymentIntentAppService paymentIntentAppService)
    : IRequestHandler<GetBankTransferPaymentIntentStatusCommand, BankTransferPaymentIntentResultModel>
{
    public async Task<BankTransferPaymentIntentResultModel> Handle(GetBankTransferPaymentIntentStatusCommand request, CancellationToken cancellationToken)
    {
        var result = await paymentIntentAppService.GetStatusAsync(request.IntentId).ConfigureAwait(false);
        return PaymentIntentModelMapper.MapIntentResult(result);
    }
}

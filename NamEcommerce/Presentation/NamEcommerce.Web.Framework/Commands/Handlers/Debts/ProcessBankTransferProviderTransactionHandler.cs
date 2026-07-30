using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.FastSales;
using NamEcommerce.Web.Framework.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class ProcessBankTransferProviderTransactionHandler(IBankTransferPaymentIntentAppService paymentIntentAppService)
    : IRequestHandler<ProcessBankTransferProviderTransactionCommand, BankTransferPaymentIntentResultModel>
{
    public async Task<BankTransferPaymentIntentResultModel> Handle(ProcessBankTransferProviderTransactionCommand request, CancellationToken cancellationToken)
    {
        var result = await paymentIntentAppService.ProcessProviderTransactionAsync(new ProcessBankTransferProviderTransactionAppDto
        {
            ReferenceCode = request.ReferenceCode,
            Amount = request.Amount,
            BankId = request.BankId,
            AccountNo = request.AccountNo,
            ProviderTransactionId = request.ProviderTransactionId,
            Source = request.Source,
            RawPayload = request.RawPayload,
            ConfirmedAtUtc = request.ConfirmedAtUtc
        }).ConfigureAwait(false);

        return PaymentIntentModelMapper.MapIntentResult(result);
    }
}

using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.FastSales;
using NamEcommerce.Web.Framework.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class ManualConfirmBankTransferPaymentIntentHandler(IBankTransferPaymentIntentAppService paymentIntentAppService)
    : IRequestHandler<ManualConfirmBankTransferPaymentIntentCommand, BankTransferPaymentIntentResultModel>
{
    public async Task<BankTransferPaymentIntentResultModel> Handle(ManualConfirmBankTransferPaymentIntentCommand request, CancellationToken cancellationToken)
    {
        var result = await paymentIntentAppService.ConfirmManuallyAsync(new ManualConfirmBankTransferPaymentIntentAppDto
        {
            IntentId = request.IntentId,
            Note = request.Note
        }).ConfigureAwait(false);

        return PaymentIntentModelMapper.MapIntentResult(result);
    }
}

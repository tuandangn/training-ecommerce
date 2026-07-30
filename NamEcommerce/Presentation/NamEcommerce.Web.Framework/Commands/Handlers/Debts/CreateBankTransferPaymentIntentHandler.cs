using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.FastSales;
using NamEcommerce.Web.Framework.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class CreateBankTransferPaymentIntentHandler(IBankTransferPaymentIntentAppService paymentIntentAppService)
    : IRequestHandler<CreateBankTransferPaymentIntentCommand, BankTransferPaymentIntentResultModel>
{
    public async Task<BankTransferPaymentIntentResultModel> Handle(CreateBankTransferPaymentIntentCommand request, CancellationToken cancellationToken)
    {
        var result = await paymentIntentAppService.CreateAsync(new CreateBankTransferPaymentIntentAppDto
        {
            Amount = request.Amount,
            CustomerId = request.CustomerId,
            Note = request.Note
        }).ConfigureAwait(false);

        return PaymentIntentModelMapper.MapIntentResult(result);
    }
}

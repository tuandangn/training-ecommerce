using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Debts;

public sealed class GetCustomerPaymentReceiptHandler(ICustomerDebtAppService debtAppService) : IRequestHandler<GetCustomerPaymentReceiptQuery, CustomerPaymentReceiptModel?>
{
    private readonly ICustomerDebtAppService _debtAppService = debtAppService;

    public async Task<CustomerPaymentReceiptModel?> Handle(GetCustomerPaymentReceiptQuery request, CancellationToken cancellationToken)
    {
        var payment = await _debtAppService.GetPaymentByIdAsync(request.PaymentId).ConfigureAwait(false);
        if (payment == null) return null;

        // Nếu payment gắn với 1 debt → load debt để lấy thông tin
        string? debtCode = null;
        if (payment.CustomerDebtId.HasValue)
        {
            var debt = await _debtAppService.GetDebtByIdAsync(payment.CustomerDebtId.Value).ConfigureAwait(false);
            debtCode = debt?.Code;
        }

        return new CustomerPaymentReceiptModel
        {
            PaymentId = payment.Id,
            PaymentCode = payment.Code,
            PaymentMethod = payment.PaymentMethod,
            PaymentType = payment.PaymentType,
            CustomerName = payment.CustomerName,
            CustomerId = payment.CustomerId,
            DebtCode = debtCode,
            OrderCode = payment.OrderCode,
            DeliveryNoteCode = payment.DeliveryNoteCode,
            Amount = payment.Amount,
            Note = payment.Note,
            PaidOn = payment.PaidOnUtc.ToLocalTime()
        };
    }
}

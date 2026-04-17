using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Debts;

public sealed class GetCustomerDebtDetailsHandler(ICustomerDebtAppService debtAppService) : IRequestHandler<GetCustomerDebtDetailsQuery, CustomerDebtDetailsModel?>
{
    private readonly ICustomerDebtAppService _debtAppService = debtAppService;

    public async Task<CustomerDebtDetailsModel?> Handle(GetCustomerDebtDetailsQuery request, CancellationToken cancellationToken)
    {
        var debt = await _debtAppService.GetDebtByIdAsync(request.Id).ConfigureAwait(false);
        if (debt == null) return null;

        return new CustomerDebtDetailsModel
        {
            Id = debt.Id,
            Code = debt.Code,
            CustomerId = debt.CustomerId,
            CustomerName = debt.CustomerName,
            DeliveryNoteCode = debt.DeliveryNoteCode,
            OrderCode = debt.OrderCode,
            OrderId = debt.OrderId,
            TotalAmount = debt.TotalAmount,
            PaidAmount = debt.PaidAmount,
            RemainingAmount = debt.RemainingAmount,
            Status = debt.Status,
            StatusName = debt.Status.ToString(),
            DueDateUtc = debt.DueDateUtc?.ToLocalTime(),
            CreatedOnUtc = debt.CreatedOnUtc.ToLocalTime(),
            Payments = debt.Payments.Select(p => new CustomerPaymentListItemModel
            {
                Id = p.Id,
                Code = p.Code,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                PaymentMethodName = p.PaymentMethod.ToString(),
                PaymentType = p.PaymentType,
                PaymentTypeName = p.PaymentType.ToString(),
                Note = p.Note,
                PaidOnUtc = p.PaidOnUtc.ToLocalTime(),
                OrderCode = p.OrderCode
            }).ToList()
        };
    }
}

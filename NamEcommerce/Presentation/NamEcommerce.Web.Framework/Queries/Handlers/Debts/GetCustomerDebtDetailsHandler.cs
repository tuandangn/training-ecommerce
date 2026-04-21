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
        var result = await _debtAppService.GetDebtsByCustomerIdAsync(request.CustomerId).ConfigureAwait(false);
        if (result == null) return null;

        var debtItems = result.Debts.Select(d => new CustomerDebtItemModel
        {
            Id = d.Id,
            Code = d.Code,
            DeliveryNoteCode = d.DeliveryNoteCode,
            OrderCode = d.OrderCode,
            OrderId = d.OrderId,
            TotalAmount = d.TotalAmount,
            PaidAmount = d.PaidAmount,
            RemainingAmount = d.RemainingAmount,
            Status = d.Status,
            DueDateUtc = d.DueDateUtc?.ToLocalTime(),
            CreatedOnUtc = d.CreatedOnUtc.ToLocalTime(),
            Payments = d.Payments.Select(p => MapPayment(p)).ToList()
        }).ToList();

        var deposits = result.Deposits.Select(p => MapPayment(p)).ToList();
        var recentPayments = result.RecentPayments.Select(p => MapPayment(p)).ToList();

        return new CustomerDebtDetailsModel
        {
            CustomerId = result.CustomerId,
            CustomerName = result.CustomerName,
            TotalDebtAmount = result.TotalDebtAmount,
            TotalPaidAmount = result.TotalPaidAmount,
            TotalRemainingAmount = result.TotalRemainingAmount,
            DepositBalance = result.DepositBalance,
            Debts = debtItems,
            Deposits = deposits,
            RecentPayments = recentPayments
        };
    }

    private static CustomerPaymentListItemModel MapPayment(Application.Contracts.Dtos.Debts.CustomerPaymentAppDto p) =>
        new()
        {
            Id = p.Id,
            Code = p.Code,
            Amount = p.Amount,
            PaymentMethod = p.PaymentMethod,
            PaymentType = p.PaymentType,
            Note = p.Note,
            PaidOnUtc = p.PaidOnUtc.ToLocalTime(),
            OrderCode = p.OrderCode,
            DeliveryNoteCode = p.DeliveryNoteCode,
            CustomerDebtId = p.CustomerDebtId
        };
}

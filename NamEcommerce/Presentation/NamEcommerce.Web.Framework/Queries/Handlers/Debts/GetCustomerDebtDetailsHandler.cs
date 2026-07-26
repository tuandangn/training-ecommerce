using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;
using NamEcommerce.Web.Framework.Services;

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
            DeliveryNoteId = d.DeliveryNoteId,
            DeliveryNoteCode = d.DeliveryNoteCode,
            OrderCode = d.OrderCode,
            OrderId = d.OrderId,
            TotalAmount = d.TotalAmount,
            PaidAmount = d.PaidAmount,
            RemainingAmount = d.RemainingAmount,
            Status = d.Status,
            DueDateUtc = DateTimeHelper.ToLocalTime(d.DueDateUtc),
            CreatedOnUtc = DateTimeHelper.ToLocalTime(d.CreatedOnUtc),
            Payments = d.Payments.Select(p => MapPayment(p)).ToList(),
            CreditNoteAllocations = d.CreditNoteAllocations.Select(MapAllocation).ToList()
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
            RecentPayments = recentPayments,
            UnappliedCreditNoteBalance = result.UnappliedCreditNoteBalance,
            UnappliedCreditNotes = result.UnappliedCreditNotes.Select(MapCreditNote).ToList()
        };
    }

    private static CustomerPaymentListItemModel MapPayment(CustomerPaymentAppDto p) =>
        new()
        {
            Id = p.Id,
            Code = p.Code,
            Amount = p.Amount,
            PaymentMethod = p.PaymentMethod,
            PaymentType = p.PaymentType,
            Note = p.Note,
            PaidOnUtc = DateTimeHelper.ToLocalTime(p.PaidOnUtc),
            OrderCode = p.OrderCode,
            DeliveryNoteCode = p.DeliveryNoteCode,
            CustomerDebtId = p.CustomerDebtId
        };

    private static CustomerCreditNoteModel MapCreditNote(CustomerCreditNoteAppDto creditNote) =>
        new()
        {
            Id = creditNote.Id,
            Code = creditNote.Code,
            SourceReturnId = creditNote.SourceReturnId,
            SourceReturnCode = creditNote.SourceReturnCode,
            Amount = creditNote.Amount,
            AppliedAmount = creditNote.AppliedAmount,
            RemainingAmount = creditNote.RemainingAmount,
            CreatedOn = DateTimeHelper.ToLocalTime(creditNote.CreatedOnUtc)
        };

    private static CreditNoteAllocationModel MapAllocation(CustomerCreditNoteAllocationAppDto allocation) =>
        new()
        {
            Id = allocation.Id,
            CreditNoteCode = allocation.CustomerCreditNoteCode,
            SourceReturnId = allocation.SourceReturnId,
            SourceReturnCode = allocation.SourceReturnCode,
            Amount = allocation.Amount,
            AppliedOn = DateTimeHelper.ToLocalTime(allocation.AppliedOnUtc),
            ReversedOn = DateTimeHelper.ToLocalTime(allocation.ReversedOnUtc),
            ReverseReason = allocation.ReverseReason
        };
}

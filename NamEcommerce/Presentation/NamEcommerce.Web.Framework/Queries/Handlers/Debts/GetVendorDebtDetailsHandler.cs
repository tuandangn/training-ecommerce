using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Debts;

public sealed class GetVendorDebtDetailsHandler(IVendorDebtAppService debtAppService) : IRequestHandler<GetVendorDebtDetailsQuery, VendorDebtDetailsModel?>
{
    private readonly IVendorDebtAppService _debtAppService = debtAppService;

    public async Task<VendorDebtDetailsModel?> Handle(GetVendorDebtDetailsQuery request, CancellationToken cancellationToken)
    {
        var result = await _debtAppService.GetDebtsByVendorIdAsync(request.VendorId).ConfigureAwait(false);
        if (result == null) return null;

        var debtItems = result.Debts.Select(d => new VendorDebtItemModel
        {
            Id = d.Id,
            Code = d.Code,
            PurchaseOrderCode = d.PurchaseOrderCode,
            PurchaseOrderId = d.PurchaseOrderId,
            TotalAmount = d.TotalAmount,
            PaidAmount = d.PaidAmount,
            RemainingAmount = d.RemainingAmount,
            Status = d.Status,
            DueDate = DateTimeHelper.ToLocalTime(d.DueDateUtc),
            CreatedOn = DateTimeHelper.ToLocalTime(d.CreatedOnUtc),
            Payments = d.Payments.Select(p => MapPayment(p)).ToList(),
            CreditNoteAllocations = d.CreditNoteAllocations.Select(MapAllocation).ToList()
        }).ToList();

        var advancePayments = result.AdvancePayments.Select(p => MapPayment(p)).ToList();
        var recentPayments = result.RecentPayments.Select(p => MapPayment(p)).ToList();

        return new VendorDebtDetailsModel
        {
            VendorId = result.VendorId,
            VendorName = result.VendorName,
            TotalDebtAmount = result.TotalDebtAmount,
            TotalPaidAmount = result.TotalPaidAmount,
            TotalRemainingAmount = result.TotalRemainingAmount,
            AdvanceBalance = result.AdvanceBalance,
            Debts = debtItems,
            AdvancePayments = advancePayments,
            RecentPayments = recentPayments,
            UnappliedCreditNoteBalance = result.UnappliedCreditNoteBalance,
            UnappliedCreditNotes = result.UnappliedCreditNotes.Select(MapCreditNote).ToList()
        };
    }

    private static VendorPaymentListItemModel MapPayment(VendorPaymentAppDto p) =>
        new()
        {
            Id = p.Id,
            Code = p.Code,
            Amount = p.Amount,
            PaymentMethod = p.PaymentMethod,
            PaymentType = p.PaymentType,
            Note = p.Note,
            PaidOn = DateTimeHelper.ToLocalTime(p.PaidOnUtc),
            PurchaseOrderCode = p.PurchaseOrderCode,
            VendorDebtId = p.VendorDebtId
        };

    private static VendorCreditNoteModel MapCreditNote(VendorCreditNoteAppDto creditNote) =>
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

    private static CreditNoteAllocationModel MapAllocation(VendorCreditNoteAllocationAppDto allocation) =>
        new()
        {
            Id = allocation.Id,
            CreditNoteCode = allocation.VendorCreditNoteCode,
            SourceReturnId = allocation.SourceReturnId,
            SourceReturnCode = allocation.SourceReturnCode,
            Amount = allocation.Amount,
            AppliedOn = DateTimeHelper.ToLocalTime(allocation.AppliedOnUtc),
            ReversedOn = DateTimeHelper.ToLocalTime(allocation.ReversedOnUtc),
            ReverseReason = allocation.ReverseReason
        };
}

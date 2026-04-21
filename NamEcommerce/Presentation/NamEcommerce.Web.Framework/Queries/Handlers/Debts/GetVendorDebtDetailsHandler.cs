using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;

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
            DueDate = d.DueDateUtc?.ToLocalTime(),
            CreatedOn = d.CreatedOnUtc.ToLocalTime(),
            Payments = d.Payments.Select(p => MapPayment(p)).ToList()
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
            RecentPayments = recentPayments
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
            PaidOn = p.PaidOnUtc.ToLocalTime(),
            PurchaseOrderCode = p.PurchaseOrderCode,
            VendorDebtId = p.VendorDebtId
        };
}

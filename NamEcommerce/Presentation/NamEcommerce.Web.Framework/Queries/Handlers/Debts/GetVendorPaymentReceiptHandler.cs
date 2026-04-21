using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Debts;

public sealed class GetVendorPaymentReceiptHandler(IVendorDebtAppService debtAppService) : IRequestHandler<GetVendorPaymentReceiptQuery, VendorPaymentReceiptModel?>
{
    private readonly IVendorDebtAppService _debtAppService = debtAppService;

    public async Task<VendorPaymentReceiptModel?> Handle(GetVendorPaymentReceiptQuery request, CancellationToken cancellationToken)
    {
        var payment = await _debtAppService.GetPaymentByIdAsync(request.PaymentId).ConfigureAwait(false);
        if (payment == null) return null;

        // Nếu payment gắn với 1 debt → load debt để lấy mã phiếu nợ
        string? debtCode = null;
        if (payment.VendorDebtId.HasValue)
        {
            var debt = await _debtAppService.GetDebtByIdAsync(payment.VendorDebtId.Value).ConfigureAwait(false);
            debtCode = debt?.Code;
        }

        return new VendorPaymentReceiptModel
        {
            PaymentId = payment.Id,
            PaymentCode = payment.Code,
            PaymentMethod = payment.PaymentMethod,
            PaymentType = payment.PaymentType,
            VendorName = payment.VendorName,
            VendorId = payment.VendorId,
            DebtCode = debtCode,
            PurchaseOrderCode = payment.PurchaseOrderCode,
            Amount = payment.Amount,
            Note = payment.Note,
            PaidOn = payment.PaidOnUtc.ToLocalTime()
        };
    }
}

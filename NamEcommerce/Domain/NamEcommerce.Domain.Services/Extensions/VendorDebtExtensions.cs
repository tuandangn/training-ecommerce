using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Shared.Dtos.Debts;

namespace NamEcommerce.Domain.Services.Extensions;

public static class VendorDebtExtensions
{
    public static VendorDebtDto ToDto(this VendorDebt debt)
        => new VendorDebtDto
        {
            Id = debt.Id,
            Code = debt.Code,
            VendorId = debt.VendorId,
            VendorName = debt.VendorName,
            VendorPhone = debt.VendorPhone,
            VendorAddress = debt.VendorAddress,
            PurchaseOrderId = debt.PurchaseOrderId,
            PurchaseOrderCode = debt.PurchaseOrderCode,
            GoodsReceiptId = debt.GoodsReceiptId,
            TotalAmount = debt.TotalAmount,
            PaidAmount = debt.PaidAmount,
            RemainingAmount = debt.RemainingAmount,
            Status = debt.Status,
            DueDateUtc = debt.DueDateUtc,
            CreatedOnUtc = debt.CreatedOnUtc,
            CreatedByUserId = debt.CreatedByUserId
        };

    public static VendorPaymentDto ToDto(this VendorPayment payment)
        => new VendorPaymentDto
        {
            Id = payment.Id,
            Code = payment.Code,
            VendorId = payment.VendorId,
            VendorName = payment.VendorName,
            VendorDebtId = payment.VendorDebtId,
            PurchaseOrderId = payment.PurchaseOrderId,
            PurchaseOrderCode = payment.PurchaseOrderCode,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            PaymentType = payment.PaymentType,
            Note = payment.Note,
            PaidOnUtc = payment.PaidOnUtc,
            RecordedByUserId = payment.RecordedByUserId,
            CreatedOnUtc = payment.CreatedOnUtc
        };
}

using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.Debts;

namespace NamEcommerce.Application.Services.Extensions;

public static class VendorDebtExtensions
{
    public static VendorDebtAppDto ToDto(this VendorDebtDto debt)
        => new()
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
            Status = (int)debt.Status,
            DueDateUtc = debt.DueDateUtc,
            CreatedOnUtc = debt.CreatedOnUtc,
            CreatedByUserId = debt.CreatedByUserId,
            Payments = debt.Payments.Select(p => p.ToDto()).ToList()
        };

    public static VendorPaymentAppDto ToDto(this VendorPaymentDto payment)
        => new()
        {
            Id = payment.Id,
            Code = payment.Code,
            VendorId = payment.VendorId,
            VendorName = payment.VendorName,
            VendorDebtId = payment.VendorDebtId,
            PurchaseOrderId = payment.PurchaseOrderId,
            PurchaseOrderCode = payment.PurchaseOrderCode,
            Amount = payment.Amount,
            PaymentMethod = (int)payment.PaymentMethod,
            PaymentType = (int)payment.PaymentType,
            Note = payment.Note,
            PaidOnUtc = payment.PaidOnUtc,
            RecordedByUserId = payment.RecordedByUserId,
            CreatedOnUtc = payment.CreatedOnUtc
        };
}

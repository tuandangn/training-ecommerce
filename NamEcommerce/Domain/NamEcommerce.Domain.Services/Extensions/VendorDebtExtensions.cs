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
            CreatedOnUtc = debt.CreatedOnUtc
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
            BankAccountId = payment.BankAccountId,
            Note = payment.Note,
            PaidOnUtc = payment.PaidOnUtc,
            RecordedByUserId = payment.RecordedByUserId,
            CreatedOnUtc = payment.CreatedOnUtc
        };

    public static VendorCreditNoteDto ToDto(this VendorCreditNote creditNote)
        => new VendorCreditNoteDto
        {
            Id = creditNote.Id,
            Code = creditNote.Code,
            VendorId = creditNote.VendorId,
            VendorName = creditNote.VendorName,
            SourceReturnId = creditNote.SourceReturnId,
            SourceReturnCode = creditNote.SourceReturnCode,
            SourceGoodsReceiptId = creditNote.SourceGoodsReceiptId,
            SourcePurchaseOrderId = creditNote.SourcePurchaseOrderId,
            Amount = creditNote.Amount,
            AppliedAmount = creditNote.AppliedAmount,
            RemainingAmount = creditNote.RemainingAmount,
            Status = creditNote.Status,
            CreatedOnUtc = creditNote.CreatedOnUtc,
            Allocations = creditNote.Allocations
                .OrderBy(a => a.AppliedOnUtc)
                .Select(a => a.ToDto())
                .ToList()
        };

    public static VendorCreditNoteAllocationDto ToDto(this VendorCreditNoteAllocation allocation)
        => new VendorCreditNoteAllocationDto
        {
            Id = allocation.Id,
            VendorCreditNoteId = allocation.VendorCreditNoteId,
            VendorCreditNoteCode = allocation.VendorCreditNoteCode,
            SourceReturnId = allocation.SourceReturnId,
            SourceReturnCode = allocation.SourceReturnCode,
            VendorDebtId = allocation.VendorDebtId,
            VendorDebtCode = allocation.VendorDebtCode,
            Amount = allocation.Amount,
            AppliedOnUtc = allocation.AppliedOnUtc,
            AppliedByUserId = allocation.AppliedByUserId,
            ReversedOnUtc = allocation.ReversedOnUtc,
            ReverseReason = allocation.ReverseReason
        };
}

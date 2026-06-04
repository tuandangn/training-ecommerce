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
            Payments = debt.Payments.Select(p => p.ToDto()).ToList(),
            CreditNoteAllocations = debt.CreditNoteAllocations.Select(a => a.ToDto()).ToList()
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

    public static VendorCreditNoteAppDto ToDto(this VendorCreditNoteDto creditNote)
        => new()
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
            Status = (int)creditNote.Status,
            CreatedOnUtc = creditNote.CreatedOnUtc,
            Allocations = creditNote.Allocations.Select(a => a.ToDto()).ToList()
        };

    public static VendorCreditNoteAllocationAppDto ToDto(this VendorCreditNoteAllocationDto allocation)
        => new()
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

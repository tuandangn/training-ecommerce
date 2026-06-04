using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.Debts;

namespace NamEcommerce.Application.Services.Extensions;

public static class DebtExtensions
{
    public static CustomerDebtAppDto ToDto(this CustomerDebtDto debt)
    {
        return new CustomerDebtAppDto
        {
            Id = debt.Id,
            Code = debt.Code,
            CustomerId = debt.CustomerId,
            CustomerName = debt.CustomerName,
            DeliveryNoteId = debt.DeliveryNoteId,
            DeliveryNoteCode = debt.DeliveryNoteCode,
            OrderId = debt.OrderId,
            OrderCode = debt.OrderCode,
            TotalAmount = debt.TotalAmount,
            PaidAmount = debt.PaidAmount,
            RemainingAmount = debt.RemainingAmount,
            Status = (int)debt.Status,
            DueDateUtc = debt.DueDateUtc,
            CreatedOnUtc = debt.CreatedOnUtc,
            Payments = debt.Payments.Select(p => p.ToDto()).ToList(),
            CreditNoteAllocations = debt.CreditNoteAllocations.Select(a => a.ToDto()).ToList()
        };
    }

    public static CustomerPaymentAppDto ToDto(this CustomerPaymentDto payment)
    {
        return new CustomerPaymentAppDto
        {
            Id = payment.Id,
            Code = payment.Code,
            CustomerId = payment.CustomerId,
            CustomerName = payment.CustomerName,
            OrderId = payment.OrderId,
            OrderCode = payment.OrderCode,
            DeliveryNoteId = payment.DeliveryNoteId,
            DeliveryNoteCode = payment.DeliveryNoteCode,
            CustomerDebtId = payment.CustomerDebtId,
            Amount = payment.Amount,
            PaymentMethod = (int)payment.PaymentMethod,
            PaymentType = (int)payment.PaymentType,
            Note = payment.Note,
            PaidOnUtc = payment.PaidOnUtc,
            RecordedByUserId = payment.RecordedByUserId,
            CreatedOnUtc = payment.CreatedOnUtc
        };
    }

    public static CustomerCreditNoteAppDto ToDto(this CustomerCreditNoteDto creditNote)
    {
        return new CustomerCreditNoteAppDto
        {
            Id = creditNote.Id,
            Code = creditNote.Code,
            CustomerId = creditNote.CustomerId,
            CustomerName = creditNote.CustomerName,
            SourceReturnId = creditNote.SourceReturnId,
            SourceReturnCode = creditNote.SourceReturnCode,
            SourceDeliveryNoteId = creditNote.SourceDeliveryNoteId,
            Amount = creditNote.Amount,
            AppliedAmount = creditNote.AppliedAmount,
            RemainingAmount = creditNote.RemainingAmount,
            Status = (int)creditNote.Status,
            CreatedOnUtc = creditNote.CreatedOnUtc,
            Allocations = creditNote.Allocations.Select(a => a.ToDto()).ToList()
        };
    }

    public static CustomerCreditNoteAllocationAppDto ToDto(this CustomerCreditNoteAllocationDto allocation)
    {
        return new CustomerCreditNoteAllocationAppDto
        {
            Id = allocation.Id,
            CustomerCreditNoteId = allocation.CustomerCreditNoteId,
            CustomerCreditNoteCode = allocation.CustomerCreditNoteCode,
            SourceReturnId = allocation.SourceReturnId,
            SourceReturnCode = allocation.SourceReturnCode,
            CustomerDebtId = allocation.CustomerDebtId,
            CustomerDebtCode = allocation.CustomerDebtCode,
            Amount = allocation.Amount,
            AppliedOnUtc = allocation.AppliedOnUtc,
            AppliedByUserId = allocation.AppliedByUserId,
            ReversedOnUtc = allocation.ReversedOnUtc,
            ReverseReason = allocation.ReverseReason
        };
    }
}

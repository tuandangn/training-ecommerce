using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Exceptions.Customers;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Common;
using Microsoft.EntityFrameworkCore;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Domain.Services.Debts;

public sealed class CustomerDebtManager(
    IRepository<CustomerDebt> debtRepository,
    IEntityDataReader<CustomerDebt> debtReader,
    IRepository<CustomerPayment> paymentRepository,
    IEntityDataReader<CustomerPayment> paymentReader,
    IRepository<CustomerCreditNote> creditNoteRepository,
    IEntityDataReader<CustomerCreditNote> creditNoteReader,
    IRepository<Customer> customerRepository,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    ICustomerLedgerManager customerLedgerManager,
    EntityCodeGenerator entityCodeGenerator,
    IOrderManager orderManager) : ICustomerDebtManager
{
    private async Task<string> GenerateDebtCodeAsync()
    {
        var prefix = $"CN-KH-{DateTime.UtcNow:yyMM}";
        return await entityCodeGenerator.NextAsync(prefix, () => debtReader.SecuredDataSource.CountAsync(d => d.Code.StartsWith(prefix)))
            .ConfigureAwait(false);
    }

    private async Task<string> GeneratePaymentCodeAsync()
    {
        var prefix = $"PT-KH-{DateTime.UtcNow:yyMM}";
        return await entityCodeGenerator.NextAsync(prefix, () => paymentReader.SecuredDataSource.CountAsync(p => p.Code.StartsWith(prefix)))
            .ConfigureAwait(false);
    }

    private async Task<string> GenerateCreditNoteCodeAsync()
    {
        var prefix = $"DC-KH-{DateTime.UtcNow:yyMM}";
        return await entityCodeGenerator.NextAsync(prefix, () => creditNoteReader.SecuredDataSource.CountAsync(c => c.Code.StartsWith(prefix)))
            .ConfigureAwait(false);
    }

    public async Task<CustomerDebtDto> CreateInitialDebtAsync(CreateInitialCustomerDebtDto dto)
    {
        dto.Verify();

        var customer = await customerRepository.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer == null) throw new CustomerIsNotFoundException(dto.CustomerId);

        var code = await GenerateDebtCodeAsync().ConfigureAwait(false);

        var debt = new CustomerDebt(
            code: code,
            customerId: customer.Id,
            customerName: customer.FullName,
            totalAmount: dto.TotalAmount
        )
        {
            CustomerAddress = customer.Address,
            CustomerPhone = customer.PhoneNumber
        };

        debt.MarkCreated();
        var inserted = await debtRepository.InsertAsync(debt).ConfigureAwait(false);

        await customerLedgerManager.RecordChargeAsync(new RecordCustomerLedgerChargeDto
        {
            CustomerId = dto.CustomerId,
            Amount = dto.TotalAmount,
            ReferenceType = CustomerLedgerReferenceType.None,
            OccurredAtUtc = inserted.CreatedOnUtc
        }).ConfigureAwait(false);

        return MapToDto(inserted);
    }

    public async Task<CustomerDebtDto> CreateDebtFromDeliveryNoteAsync(CreateCustomerDebtDto dto)
    {
        dto.Verify();

        var existing = await debtReader.DataSource.FirstOrDefaultAsync(d => d.DeliveryNoteId == dto.DeliveryNoteId).ConfigureAwait(false);
        if (existing != null)
        {
            if (dto.TotalAmount > existing.TotalAmount)
            {
                existing.UpdateTotalAmount(dto.TotalAmount);
                await debtRepository.UpdateAsync(existing).ConfigureAwait(false);
            }

            return MapToDto(existing);
        }

        var customer = await customerRepository.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        var deliveryNote = await deliveryNoteReader.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false);

        if (customer == null) throw new CustomerIsNotFoundException(dto.CustomerId);
        if (deliveryNote == null) throw new DeliveryNoteNotFoundException(dto.DeliveryNoteId);

        var code = await GenerateDebtCodeAsync().ConfigureAwait(false);

        var debt = new CustomerDebt(
            code: code,
            customerId: customer.Id,
            customerName: customer.FullName,
            deliveryNoteId: deliveryNote.Id,
            deliveryNoteCode: deliveryNote.Code,
            orderId: deliveryNote.OrderId,
            orderCode: deliveryNote.OrderCode ?? string.Empty,
            totalAmount: dto.TotalAmount,
            dueDateUtc: dto.DueDateUtc
        )
        {
            CustomerAddress = customer.Address,
            CustomerPhone = customer.PhoneNumber
        };

        debt.MarkCreated();
        var inserted = await debtRepository.InsertAsync(debt).ConfigureAwait(false);

        return MapToDto(inserted);
    }

    public async Task<CustomerPaymentDto> RecordPaymentAsync(CreateCustomerPaymentDto dto)
    {
        dto.Verify();

        var customer = await customerRepository.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer == null) throw new CustomerIsNotFoundException(dto.CustomerId);

        var code = await GeneratePaymentCodeAsync().ConfigureAwait(false);

        var payment = new CustomerPayment(
            code: code,
            customerId: customer.Id,
            customerName: customer.FullName,
            amount: dto.Amount,
            paymentMethod: dto.PaymentMethod,
            paymentType: dto.PaymentType,
            paidOnUtc: dto.PaidOnUtc,
            recordedByUserId: dto.RecordedByUserId,
            note: dto.Note
        )
        {
            OrderId = dto.OrderId,
            DeliveryNoteId = dto.DeliveryNoteId,
            CustomerDebtId = dto.CustomerDebtId,
            BankAccountId = dto.PaymentMethod == PaymentMethod.BankTransfer ? dto.BankAccountId : null
        };
        payment.MarkCreated();

        var inserted = await paymentRepository.InsertAsync(payment).ConfigureAwait(false);

        if (dto.OrderId.HasValue)
            await orderManager.MarkOrderHasPayment(dto.OrderId.Value, dto.Amount, null).ConfigureAwait(false);

        await customerLedgerManager.RecordPaymentAsync(new RecordCustomerLedgerPaymentDto
        {
            CustomerId = dto.CustomerId,
            Amount = inserted.Amount,
            ReferenceId = inserted.Id,
            ReferenceCode = inserted.Code,
            OccurredAtUtc = dto.PaidOnUtc,
            CreatedByUserId = dto.RecordedByUserId
        }).ConfigureAwait(false);

        return MapToPaymentDto(inserted);
    }

    public async Task<CustomerDebtDto?> GetDebtByIdAsync(Guid id)
    {
        var debt = await debtRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (debt == null) return null;

        var payments = paymentReader.DataSource
            .Where(p => p.CustomerDebtId == id)
            .OrderBy(p => p.PaidOnUtc)
            .ToList();

        var allocations = await GetCreditNoteAllocationsByDebtIdAsync(id).ConfigureAwait(false);

        var dto = MapToDto(debt);
        return dto with
        {
            Payments = payments.Select(MapToPaymentDto).ToList(),
            CreditNoteAllocations = allocations
        };
    }

    public async Task<CustomerPaymentDto?> GetPaymentByIdAsync(Guid paymentId)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId).ConfigureAwait(false);
        return payment == null ? null : MapToPaymentDto(payment);
    }

    public async Task<CustomerDebtSummaryDto?> GetCustomerDebtSummaryAsync(Guid customerId)
    {
        var debts = await debtReader.DataSource
            .Where(d => d.CustomerId == customerId)
            .ToListAsync().ConfigureAwait(false);

        if (debts.Count == 0) return null;

        var customerName = debts[0].CustomerName;

        return new CustomerDebtSummaryDto
        {
            CustomerId = customerId,
            CustomerName = customerName,
            TotalDebtAmount = debts.Sum(d => d.TotalAmount),
            TotalPaidAmount = debts.Sum(d => d.PaidAmount),
            TotalRemainingAmount = debts.Sum(d => d.RemainingAmount),
            DebtCount = debts.Count
        };
    }

    public async Task<IList<CustomerPaymentDto>> RecordFlexiblePaymentForCustomerAsync(CreateCustomerPaymentDto dto)
    {
        var payment = await RecordPaymentAsync(dto).ConfigureAwait(false);
        return [payment];
    }

    public async Task<CustomerCreditNoteDto> ApplyCreditNoteFromCustomerReturnAsync(
        Guid customerId, Guid returnId, string returnCode,
        Guid? sourceDeliveryNoteId, decimal amount)
    {
        if (amount <= 0)
            throw new NamEcommerceDomainException("Error.CreditNote.AmountMustBePositive");

        var existing = await creditNoteReader.DataSource
            .FirstOrDefaultAsync(c => c.SourceReturnId == returnId && c.Status != CreditNoteStatus.Cancelled)
            .ConfigureAwait(false);
        if (existing is not null)
            return MapToCreditNoteDto(existing);

        var customer = await customerRepository.GetByIdAsync(customerId).ConfigureAwait(false);
        if (customer == null) throw new CustomerIsNotFoundException(customerId);

        var code = await GenerateCreditNoteCodeAsync().ConfigureAwait(false);
        var sourceId = sourceDeliveryNoteId is { } id && id != Guid.Empty ? id : (Guid?)null;
        var creditNote = new CustomerCreditNote(code, customer.Id, customer.FullName,
            returnId, returnCode, sourceId, amount);

        var inserted = await creditNoteRepository.InsertAsync(creditNote).ConfigureAwait(false);
        return MapToCreditNoteDto(inserted);
    }

    public async Task<IPagedDataDto<CustomerDebtSummaryDto>> GetCustomersWithDebtsAsync(int pageIndex = 0, int pageSize = 15, string? keywords = null)
    {
        var allDebts = await debtReader.DataSource.ToListAsync().ConfigureAwait(false);

        var groups = allDebts
            .GroupBy(d => d.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                CustomerName = g.First().CustomerName,
                CustomerPhone = g.First().CustomerPhone,
                CustomerAddress = g.First().CustomerAddress,
                TotalDebtAmount = g.Sum(d => d.TotalAmount),
                TotalPaidAmount = g.Sum(d => d.PaidAmount),
                TotalRemainingAmount = g.Sum(d => d.RemainingAmount),
                DebtCount = g.Count()
            })
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(keywords))
            groups = groups.Where(g => g.CustomerName.Contains(keywords, StringComparison.OrdinalIgnoreCase));

        var sorted = groups.OrderBy(g => g.CustomerName).ToList();
        var total = sorted.Count;
        var page = sorted.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        var results = new List<CustomerDebtSummaryDto>();
        foreach (var item in page)
        {
            var depositBalance = await paymentReader.DataSource
                .Where(p => p.CustomerId == item.CustomerId
                         && (p.PaymentType == PaymentType.Deposit || p.PaymentType == PaymentType.General)
                         && p.AppliedAmount < p.Amount)
                .SumAsync(p => p.Amount - p.AppliedAmount)
                .ConfigureAwait(false);

            results.Add(new CustomerDebtSummaryDto
            {
                CustomerId = item.CustomerId,
                CustomerName = item.CustomerName,
                CustomerPhone = item.CustomerPhone,
                CustomerAddress = item.CustomerAddress,
                TotalDebtAmount = item.TotalDebtAmount,
                TotalPaidAmount = item.TotalPaidAmount,
                TotalRemainingAmount = item.TotalRemainingAmount,
                DepositBalance = depositBalance,
                DebtCount = item.DebtCount
            });
        }

        return PagedDataDto.Create(results, pageIndex, pageSize, total);
    }

    public async Task<CustomerDebtsByCustomerDto?> GetDebtsByCustomerIdAsync(Guid customerId)
    {
        var debts = await debtReader.DataSource
            .Where(d => d.CustomerId == customerId)
            .OrderByDescending(d => d.CreatedOnUtc)
            .ToListAsync().ConfigureAwait(false);

        if (debts.Count == 0) return null;

        var debtDtos = new List<CustomerDebtDto>();
        foreach (var debt in debts)
        {
            var payments = await paymentReader.DataSource
                .Where(p => p.CustomerDebtId == debt.Id)
                .OrderBy(p => p.PaidOnUtc)
                .ToListAsync().ConfigureAwait(false);
            var allocations = await GetCreditNoteAllocationsByDebtIdAsync(debt.Id).ConfigureAwait(false);
            debtDtos.Add(MapToDto(debt) with
            {
                Payments = payments.Select(MapToPaymentDto).ToList(),
                CreditNoteAllocations = allocations
            });
        }

        var deposits = await paymentReader.DataSource
            .Where(p => p.CustomerId == customerId
                     && (p.PaymentType == PaymentType.Deposit || p.PaymentType == PaymentType.General)
                     && p.AppliedAmount < p.Amount)
            .OrderByDescending(p => p.PaidOnUtc)
            .ToListAsync().ConfigureAwait(false);

        var recentPayments = await paymentReader.DataSource
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.PaidOnUtc)
            .Take(20)
            .ToListAsync().ConfigureAwait(false);

        var depositBalance = deposits.Sum(p => p.Amount - p.AppliedAmount);
        var unappliedCreditNotes = await creditNoteReader.DataSource
            .Where(c => c.CustomerId == customerId
                && c.Status != CreditNoteStatus.Cancelled
                && c.RemainingAmount > 0)
            .OrderByDescending(c => c.CreatedOnUtc)
            .ToListAsync().ConfigureAwait(false);

        return new CustomerDebtsByCustomerDto
        {
            CustomerId = customerId,
            CustomerName = debts[0].CustomerName,
            TotalDebtAmount = debts.Sum(d => d.TotalAmount),
            TotalPaidAmount = debts.Sum(d => d.PaidAmount),
            TotalRemainingAmount = debts.Sum(d => d.RemainingAmount),
            DepositBalance = depositBalance,
            Debts = debtDtos,
            Deposits = deposits.Select(MapToPaymentDto).ToList(),
            RecentPayments = recentPayments.Select(MapToPaymentDto).ToList(),
            UnappliedCreditNoteBalance = unappliedCreditNotes.Sum(c => c.RemainingAmount),
            UnappliedCreditNotes = unappliedCreditNotes.Select(MapToCreditNoteDto).ToList()
        };
    }

    public async Task<IPagedDataDto<CustomerDebtDto>> GetDebtsAsync(int pageIndex = 0, int pageSize = 15, Guid? customerId = null, string? keywords = null)
    {
        var query = debtReader.DataSource;
        if (customerId.HasValue)
            query = query.Where(d => d.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(keywords))
            query = query.Where(d => d.Code.Contains(keywords)
                || d.CustomerName.Contains(keywords)
                || d.OrderCode.Contains(keywords)
                || d.DeliveryNoteCode.Contains(keywords));

        query = query.OrderByDescending(d => d.CreatedOnUtc);

        var total = await query.CountAsync().ConfigureAwait(false);
        var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync().ConfigureAwait(false);

        return PagedDataDto.Create(items.Select(MapToDto).ToList(), pageIndex, pageSize, total);
    }

    public async Task<IPagedDataDto<CustomerPaymentDto>> GetPaymentsAsync(int pageIndex = 0, int pageSize = 15, Guid? customerId = null, Guid? orderId = null)
    {
        var query = paymentReader.DataSource;
        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);
        if(orderId.HasValue)
            query = query.Where(p => p.OrderId == orderId.Value);

        query = query.OrderByDescending(p => p.CreatedOnUtc);

        var total = await query.CountAsync().ConfigureAwait(false);
        var items = await query.Skip(pageIndex * pageSize).Take(pageSize)
            .ToListAsync().ConfigureAwait(false);

        return PagedDataDto.Create(items.Select(MapToPaymentDto).ToList(), pageIndex, pageSize, total);
    }

    public Task<decimal> GetTotalPaidByOrderAsync(Guid orderId)
        => paymentReader.DataSource.Where(p => p.OrderId == orderId).SumAsync(p => p.Amount);

    public Task<decimal> GetTotalDebtByOrderAsync(Guid orderId)
        => debtReader.DataSource.Where(p => p.OrderId == orderId).SumAsync(p => p.TotalAmount);

    public Task<decimal> GetTotalPaidByDeliveryNoteAsync(Guid deliveryNoteId)
        => paymentReader.DataSource.Where(p => p.DeliveryNoteId == deliveryNoteId).SumAsync(p => p.Amount);

    private static CustomerDebtDto MapToDto(CustomerDebt debt)
    {
        return new CustomerDebtDto
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
            Status = debt.Status,
            DueDateUtc = debt.DueDateUtc,
            CreatedOnUtc = debt.CreatedOnUtc
        };
    }

    private async Task<IList<CustomerCreditNoteAllocationDto>> GetCreditNoteAllocationsByDebtIdAsync(Guid debtId)
        => (await creditNoteReader.DataSource
            .SelectMany(c => c.Allocations)
            .Where(a => a.CustomerDebtId == debtId)
            .OrderBy(a => a.AppliedOnUtc)
            .ToListAsync().ConfigureAwait(false))
            .Select(MapToCreditNoteAllocationDto)
            .ToList();

    private static CustomerCreditNoteDto MapToCreditNoteDto(CustomerCreditNote creditNote)
    {
        return new CustomerCreditNoteDto
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
            Status = creditNote.Status,
            CreatedOnUtc = creditNote.CreatedOnUtc,
            Allocations = creditNote.Allocations
                .OrderBy(a => a.AppliedOnUtc)
                .Select(MapToCreditNoteAllocationDto)
                .ToList()
        };
    }

    private static CustomerCreditNoteAllocationDto MapToCreditNoteAllocationDto(CustomerCreditNoteAllocation allocation)
    {
        return new CustomerCreditNoteAllocationDto
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

    private static CustomerPaymentDto MapToPaymentDto(CustomerPayment payment)
    {
        return new CustomerPaymentDto
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
            PaymentMethod = payment.PaymentMethod,
            PaymentType = payment.PaymentType,
            BankAccountId = payment.BankAccountId,
            Note = payment.Note,
            PaidOnUtc = payment.PaidOnUtc,
            RecordedByUserId = payment.RecordedByUserId,
            CreatedOnUtc = payment.CreatedOnUtc
        };
    }
}

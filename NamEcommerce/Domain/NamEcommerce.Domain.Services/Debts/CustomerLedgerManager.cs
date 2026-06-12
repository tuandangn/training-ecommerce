using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Domain.Services.Debts;

public sealed class CustomerLedgerManager(
    IDbContext dbContext,
    IRepository<CustomerLedgerEntry> entryRepository,
    IEntityDataReader<CustomerLedgerEntry> entryReader,
    IRepository<CustomerAccountBalance> balanceRepository,
    IEntityDataReader<CustomerAccountBalance> balanceReader,
    IEntityDataReader<Customer> customerReader) : ICustomerLedgerManager
{
    public async Task<CustomerLedgerEntryDto> RecordChargeAsync(RecordCustomerLedgerChargeDto dto)
    {
        dto.Verify();

        var entryType = dto.ReferenceType == CustomerLedgerReferenceType.DeliveryNote
            ? CustomerLedgerEntryType.DeliveryNoteCharge
            : CustomerLedgerEntryType.OpeningBalance;

        if (dto.ReferenceId.HasValue)
        {
            var existing = entryReader.DataSource
                .FirstOrDefault(e => e.CustomerId == dto.CustomerId
                    && e.EntryType == entryType
                    && e.ReferenceId == dto.ReferenceId.Value);
            if (existing is not null) return MapToDto(existing);
        }

        var entry = new CustomerLedgerEntry(
            customerId: dto.CustomerId,
            entryType: entryType,
            amount: dto.Amount,
            referenceType: dto.ReferenceType,
            referenceId: dto.ReferenceId,
            referenceCode: dto.ReferenceCode,
            note: dto.Note,
            occurredAtUtc: dto.OccurredAtUtc == default ? DateTime.UtcNow : dto.OccurredAtUtc,
            createdByUserId: dto.CreatedByUserId);

        entry.MarkRecorded();
        return await PersistEntryAsync(entry).ConfigureAwait(false);
    }

    public async Task<CustomerLedgerEntryDto> RecordSurchargeAsync(RecordCustomerLedgerChargeDto dto)
    {
        dto.Verify();

        if (dto.ReferenceId.HasValue)
        {
            var existing = entryReader.DataSource
                .FirstOrDefault(e => e.CustomerId == dto.CustomerId
                    && e.EntryType == CustomerLedgerEntryType.Surcharge
                    && e.ReferenceId == dto.ReferenceId.Value);
            if (existing is not null) return MapToDto(existing);
        }

        var entry = new CustomerLedgerEntry(
            customerId: dto.CustomerId,
            entryType: CustomerLedgerEntryType.Surcharge,
            amount: dto.Amount,
            referenceType: dto.ReferenceType,
            referenceId: dto.ReferenceId,
            referenceCode: dto.ReferenceCode,
            note: dto.Note,
            occurredAtUtc: dto.OccurredAtUtc == default ? DateTime.UtcNow : dto.OccurredAtUtc,
            createdByUserId: dto.CreatedByUserId);

        entry.MarkRecorded();
        return await PersistEntryAsync(entry).ConfigureAwait(false);
    }

    public async Task<CustomerLedgerEntryDto> RecordPaymentAsync(RecordCustomerLedgerPaymentDto dto)
    {
        dto.Verify();

        if (dto.ReferenceId.HasValue)
        {
            var existing = entryReader.DataSource
                .FirstOrDefault(e => e.CustomerId == dto.CustomerId
                    && e.EntryType == CustomerLedgerEntryType.Payment
                    && e.ReferenceId == dto.ReferenceId.Value);
            if (existing is not null) return MapToDto(existing);
        }

        var entry = new CustomerLedgerEntry(
            customerId: dto.CustomerId,
            entryType: CustomerLedgerEntryType.Payment,
            amount: -dto.Amount,
            referenceType: CustomerLedgerReferenceType.CustomerPayment,
            referenceId: dto.ReferenceId,
            referenceCode: dto.ReferenceCode,
            note: dto.Note,
            occurredAtUtc: dto.OccurredAtUtc == default ? DateTime.UtcNow : dto.OccurredAtUtc,
            createdByUserId: dto.CreatedByUserId);

        entry.MarkRecorded();
        return await PersistEntryAsync(entry).ConfigureAwait(false);
    }

    public async Task<CustomerLedgerEntryDto> RecordReturnCreditAsync(RecordCustomerLedgerReturnCreditDto dto)
    {
        dto.Verify();

        if (dto.ReferenceId.HasValue)
        {
            var existing = entryReader.DataSource
                .FirstOrDefault(e => e.CustomerId == dto.CustomerId
                    && e.EntryType == CustomerLedgerEntryType.ReturnCredit
                    && e.ReferenceId == dto.ReferenceId.Value);
            if (existing is not null) return MapToDto(existing);
        }

        var entry = new CustomerLedgerEntry(
            customerId: dto.CustomerId,
            entryType: CustomerLedgerEntryType.ReturnCredit,
            amount: -dto.Amount,
            referenceType: CustomerLedgerReferenceType.CustomerReturn,
            referenceId: dto.ReferenceId,
            referenceCode: dto.ReferenceCode,
            note: dto.Note,
            occurredAtUtc: dto.OccurredAtUtc == default ? DateTime.UtcNow : dto.OccurredAtUtc,
            createdByUserId: dto.CreatedByUserId);

        entry.MarkRecorded();
        return await PersistEntryAsync(entry).ConfigureAwait(false);
    }

    public async Task<CustomerLedgerEntryDto> RecordRefundPayoutAsync(RecordCustomerLedgerRefundPayoutDto dto)
    {
        dto.Verify();

        if (dto.ReferenceId.HasValue)
        {
            var existing = entryReader.DataSource
                .FirstOrDefault(e => e.CustomerId == dto.CustomerId
                    && e.EntryType == CustomerLedgerEntryType.RefundPayout
                    && e.ReferenceId == dto.ReferenceId.Value);
            if (existing is not null) return MapToDto(existing);
        }

        var entry = new CustomerLedgerEntry(
            customerId: dto.CustomerId,
            entryType: CustomerLedgerEntryType.RefundPayout,
            amount: dto.Amount,
            referenceType: CustomerLedgerReferenceType.CustomerRefund,
            referenceId: dto.ReferenceId,
            referenceCode: dto.ReferenceCode,
            note: dto.Note,
            occurredAtUtc: dto.OccurredAtUtc == default ? DateTime.UtcNow : dto.OccurredAtUtc,
            createdByUserId: dto.CreatedByUserId);

        entry.MarkRecorded();
        return await PersistEntryAsync(entry).ConfigureAwait(false);
    }

    public async Task<CustomerLedgerEntryDto> RecordCorrectionAsync(RecordCustomerCorrectionDto dto)
    {
        dto.Verify();

        var entry = new CustomerLedgerEntry(
            customerId: dto.CustomerId,
            entryType: CustomerLedgerEntryType.Correction,
            amount: dto.Amount,
            referenceType: CustomerLedgerReferenceType.None,
            referenceId: null,
            referenceCode: null,
            note: dto.Note,
            occurredAtUtc: dto.OccurredAtUtc == default ? DateTime.UtcNow : dto.OccurredAtUtc,
            createdByUserId: dto.CreatedByUserId);

        entry.MarkRecorded();
        return await PersistEntryAsync(entry).ConfigureAwait(false);
    }

    public Task<decimal> GetBalanceAsync(Guid customerId)
    {
        var balance = balanceReader.DataSource
            .Where(b => b.CustomerId == customerId)
            .Select(b => b.Balance)
            .FirstOrDefault();
        return Task.FromResult(balance);
    }

    public Task<IPagedDataDto<CustomerLedgerStatementEntryDto>> GetStatementAsync(
        Guid customerId,
        DateTime? from = null,
        DateTime? to = null,
        int pageIndex = 0,
        int pageSize = 30)
    {
        var query = entryReader.DataSource.Where(e => e.CustomerId == customerId);

        if (from.HasValue) query = query.Where(e => e.OccurredAtUtc >= from.Value);
        if (to.HasValue) query = query.Where(e => e.OccurredAtUtc <= to.Value);

        query = query.OrderBy(e => e.OccurredAtUtc).ThenBy(e => e.CreatedOnUtc);

        var total = query.Count();
        var items = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        var runningBalance = pageIndex == 0
            ? 0m
            : entryReader.DataSource
                .Where(e => e.CustomerId == customerId
                    && (from == null || e.OccurredAtUtc >= from.Value)
                    && (to == null || e.OccurredAtUtc <= to.Value))
                .OrderBy(e => e.OccurredAtUtc)
                .ThenBy(e => e.CreatedOnUtc)
                .Take(pageIndex * pageSize)
                .Sum(e => e.Amount);

        var result = new List<CustomerLedgerStatementEntryDto>(items.Count);
        foreach (var item in items)
        {
            runningBalance += item.Amount;
            result.Add(new CustomerLedgerStatementEntryDto
            {
                EntryId = item.Id,
                EntryType = item.EntryType,
                Amount = item.Amount,
                RunningBalance = runningBalance,
                ReferenceType = item.ReferenceType,
                ReferenceId = item.ReferenceId,
                ReferenceCode = item.ReferenceCode,
                Note = item.Note,
                OccurredAtUtc = item.OccurredAtUtc
            });
        }

        return Task.FromResult(PagedDataDto.Create(result, pageIndex, pageSize, total));
    }

    public Task<IPagedDataDto<CustomerAccountBalanceDto>> GetBalancesAsync(
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15)
    {
        var balanceQuery = balanceReader.DataSource;

        var joined = from b in balanceQuery
                     join c in customerReader.DataSource on b.CustomerId equals c.Id
                     select new { b, c };

        if (!string.IsNullOrWhiteSpace(keywords))
            joined = joined.Where(x => x.c.FullName.Value.Contains(keywords)
                || x.c.PhoneNumber.Contains(keywords));

        joined = joined.OrderBy(x => x.c.FullName.Value);

        var total = joined.Count();
        var items = joined.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        var result = items.Select(x => new CustomerAccountBalanceDto
        {
            CustomerId = x.b.CustomerId,
            CustomerName = x.c.FullName.Value,
            CustomerPhone = x.c.PhoneNumber,
            Balance = x.b.Balance,
            LastEntryOnUtc = x.b.LastEntryOnUtc
        }).ToList();

        return Task.FromResult(PagedDataDto.Create(result, pageIndex, pageSize, total));
    }

    private async Task<CustomerLedgerEntryDto> PersistEntryAsync(CustomerLedgerEntry entry)
    {
        await using var transaction = await dbContext.BeginTransactionAsync().ConfigureAwait(false);

        var inserted = await entryRepository.InsertAsync(entry).ConfigureAwait(false);
        await UpsertBalanceAsync(entry.CustomerId, entry.Amount, entry.OccurredAtUtc).ConfigureAwait(false);

        await transaction.CommitAsync().ConfigureAwait(false);
        return MapToDto(inserted);
    }

    private async Task UpsertBalanceAsync(Guid customerId, decimal delta, DateTime occurredAtUtc)
    {
        var existingId = balanceReader.DataSource
            .Where(b => b.CustomerId == customerId)
            .Select(b => b.Id)
            .FirstOrDefault();

        if (existingId != Guid.Empty)
        {
            var balance = await balanceRepository.GetByIdAsync(existingId).ConfigureAwait(false);
            if (balance is not null)
            {
                balance.Apply(delta, occurredAtUtc);
                await balanceRepository.UpdateAsync(balance).ConfigureAwait(false);
                return;
            }
        }

        var newBalance = new CustomerAccountBalance(customerId, delta, occurredAtUtc);
        await balanceRepository.InsertAsync(newBalance).ConfigureAwait(false);
    }

    private static CustomerLedgerEntryDto MapToDto(CustomerLedgerEntry entry) => new()
    {
        Id = entry.Id,
        CustomerId = entry.CustomerId,
        EntryType = entry.EntryType,
        Amount = entry.Amount,
        ReferenceType = entry.ReferenceType,
        ReferenceId = entry.ReferenceId,
        ReferenceCode = entry.ReferenceCode,
        Note = entry.Note,
        OccurredAtUtc = entry.OccurredAtUtc,
        CreatedByUserId = entry.CreatedByUserId,
        CreatedOnUtc = entry.CreatedOnUtc
    };

    public Task<CustomerAccountBalanceDto?> GetCustomerSummaryAsync(Guid customerId)
    {
        var result = (from b in balanceReader.DataSource
                      where b.CustomerId == customerId
                      join c in customerReader.DataSource on b.CustomerId equals c.Id
                      select new CustomerAccountBalanceDto
                      {
                          CustomerId = b.CustomerId,
                          CustomerName = c.FullName.Value,
                          CustomerPhone = c.PhoneNumber,
                          Balance = b.Balance,
                          LastEntryOnUtc = b.LastEntryOnUtc
                      }).FirstOrDefault();

        return Task.FromResult(result);
    }
}

using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Domain.Services.Debts;

public sealed class VendorLedgerManager(
    IUnitOfWork unitOfWork, IDbContext dbContext,
    IRepository<VendorLedgerEntry> entryRepository,
    IEntityDataReader<VendorLedgerEntry> entryReader,
    IRepository<VendorAccountBalance> balanceRepository,
    IEntityDataReader<VendorAccountBalance> balanceReader,
    IEntityDataReader<Vendor> vendorReader) : IVendorLedgerManager
{
    public async Task<VendorLedgerEntryDto> RecordChargeAsync(RecordVendorLedgerChargeDto dto)
    {
        dto.Verify();

        var entryType = dto.ReferenceType == VendorLedgerReferenceType.GoodsReceipt
            ? VendorLedgerEntryType.GoodsReceiptCharge
            : VendorLedgerEntryType.OpeningBalance;

        if (dto.ReferenceId.HasValue)
        {
            var existing = await entryReader.DataSource
                .FirstOrDefaultAsync(e => e.VendorId == dto.VendorId
                    && e.EntryType == entryType
                    && e.ReferenceId == dto.ReferenceId.Value)
                .ConfigureAwait(false);
            if (existing is not null) return MapToDto(existing);
        }

        var entry = new VendorLedgerEntry(dto.VendorId, entryType,
            dto.Amount, dto.ReferenceType, dto.ReferenceId, dto.ReferenceCode,
            dto.Note, dto.OccurredAtUtc == default ? DateTime.UtcNow : dto.OccurredAtUtc,
            dto.CreatedByUserId);

        entry.MarkRecorded();
        return await PersistEntryAsync(entry).ConfigureAwait(false);
    }

    public async Task<VendorLedgerEntryDto> RecordPaymentAsync(RecordVendorLedgerPaymentDto dto)
    {
        dto.Verify();

        if (dto.ReferenceId.HasValue)
        {
            var existing = await entryReader.DataSource
                .FirstOrDefaultAsync(e => e.VendorId == dto.VendorId
                    && e.EntryType == VendorLedgerEntryType.Payment
                    && e.ReferenceId == dto.ReferenceId.Value)
                .ConfigureAwait(false);
            if (existing is not null) return MapToDto(existing);
        }

        var entry = new VendorLedgerEntry(dto.VendorId, VendorLedgerEntryType.Payment,
            -dto.Amount, VendorLedgerReferenceType.VendorPayment, dto.ReferenceId,
            dto.ReferenceCode, dto.Note, dto.OccurredAtUtc == default ? DateTime.UtcNow : dto.OccurredAtUtc,
            dto.CreatedByUserId);

        entry.MarkRecorded();
        return await PersistEntryAsync(entry).ConfigureAwait(false);
    }

    public async Task<VendorLedgerEntryDto> RecordReturnCreditAsync(RecordVendorLedgerReturnCreditDto dto)
    {
        dto.Verify();

        if (dto.ReferenceId.HasValue)
        {
            var existing = await entryReader.TrackingDataSource
                .FirstOrDefaultAsync(e => e.VendorId == dto.VendorId
                    && e.EntryType == VendorLedgerEntryType.ReturnCredit
                    && e.ReferenceId == dto.ReferenceId.Value)
                .ConfigureAwait(false);
            if (existing is not null) return MapToDto(existing);
        }

        var entry = new VendorLedgerEntry(dto.VendorId, VendorLedgerEntryType.ReturnCredit,
            -dto.Amount, VendorLedgerReferenceType.VendorReturn, dto.ReferenceId, dto.ReferenceCode,
            dto.Note, dto.OccurredAtUtc == default ? DateTime.UtcNow : dto.OccurredAtUtc, dto.CreatedByUserId);

        entry.MarkRecorded();
        return await PersistEntryAsync(entry).ConfigureAwait(false);
    }

    public async Task<VendorLedgerEntryDto> RecordRefundReceiptAsync(RecordVendorLedgerRefundReceiptDto dto)
    {
        dto.Verify();

        if (dto.ReferenceId.HasValue)
        {
            var existing = await entryReader.DataSource
                .FirstOrDefaultAsync(e => e.VendorId == dto.VendorId
                    && e.EntryType == VendorLedgerEntryType.RefundReceipt
                    && e.ReferenceId == dto.ReferenceId.Value)
                .ConfigureAwait(false);
            if (existing is not null) return MapToDto(existing);
        }

        var entry = new VendorLedgerEntry(dto.VendorId, VendorLedgerEntryType.RefundReceipt,
            dto.Amount, VendorLedgerReferenceType.VendorRefund, dto.ReferenceId, dto.ReferenceCode,
            dto.Note, dto.OccurredAtUtc == default ? DateTime.UtcNow : dto.OccurredAtUtc, dto.CreatedByUserId);

        entry.MarkRecorded();
        return await PersistEntryAsync(entry).ConfigureAwait(false);
    }

    public async Task<VendorLedgerEntryDto> RecordCorrectionAsync(RecordVendorCorrectionDto dto)
    {
        dto.Verify();

        var entry = new VendorLedgerEntry(dto.VendorId, VendorLedgerEntryType.Correction, dto.Amount,
            VendorLedgerReferenceType.None, null, null, dto.Note,
            dto.OccurredAtUtc == default ? DateTime.UtcNow : dto.OccurredAtUtc, dto.CreatedByUserId);

        entry.MarkRecorded();
        return await PersistEntryAsync(entry).ConfigureAwait(false);
    }

    public Task<decimal> GetBalanceAsync(Guid vendorId)
    {
        var balance = balanceReader.DataSource
            .Where(b => b.VendorId == vendorId)
            .Select(b => b.Balance)
            .FirstOrDefaultAsync();
        return balance;
    }

    public async Task<IPagedDataDto<VendorLedgerStatementEntryDto>> GetStatementAsync(
        Guid vendorId, DateTime? from = null, DateTime? to = null, int pageIndex = 0, int pageSize = 30)
    {
        var query = entryReader.DataSource.Where(e => e.VendorId == vendorId);

        if (from.HasValue) query = query.Where(e => e.OccurredAtUtc >= from.Value);
        if (to.HasValue) query = query.Where(e => e.OccurredAtUtc <= to.Value);

        int? total = pageIndex == 0 && pageSize == int.MaxValue
            ? null : await query.CountAsync().ConfigureAwait(false);

        if (total.HasValue && total == 0)
            return PagedDataDto.Create(Enumerable.Empty<VendorLedgerStatementEntryDto>(), 0, pageSize, 0);

        query = query.OrderBy(e => e.OccurredAtUtc).ThenBy(e => e.CreatedOnUtc);
        var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync().ConfigureAwait(false);

        var runningBalance = pageIndex == 0
            ? 0m
            : await query.Take(pageIndex * pageSize).SumAsync(e => e.Amount).ConfigureAwait(false);

        var result = new List<VendorLedgerStatementEntryDto>(items.Count);
        foreach (var item in items)
        {
            runningBalance += item.Amount;
            result.Add(new VendorLedgerStatementEntryDto
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

        return PagedDataDto.Create(result, pageIndex, pageSize, total);
    }

    public async Task<IPagedDataDto<VendorAccountBalanceDto>> GetBalancesAsync(
        string? keywords = null, int pageIndex = 0, int pageSize = 15)
    {
        var joined = from b in balanceReader.DataSource
                     join v in vendorReader.DataSource on b.VendorId equals v.Id
                     select new { b, v };

        if (!string.IsNullOrWhiteSpace(keywords))
            joined = joined.Where(x => x.v.Name.Contains(keywords)
                || x.v.PhoneNumber.Contains(keywords));

        joined = joined.OrderBy(x => x.v.Name);

        var total = await joined.CountAsync().ConfigureAwait(false);
        var items = await joined.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync().ConfigureAwait(false);

        var result = items.Select(x => new VendorAccountBalanceDto
        {
            VendorId = x.b.VendorId,
            VendorName = x.v.Name,
            VendorPhone = x.v.PhoneNumber,
            Balance = x.b.Balance,
            LastEntryOnUtc = x.b.LastEntryOnUtc
        }).ToList();

        return PagedDataDto.Create(result, pageIndex, pageSize, total);
    }

    public Task<VendorAccountBalanceDto?> GetVendorSummaryAsync(Guid vendorId)
    {
        var result = (from b in balanceReader.DataSource
                      where b.VendorId == vendorId
                      join v in vendorReader.DataSource on b.VendorId equals v.Id
                      select new VendorAccountBalanceDto
                      {
                          VendorId = b.VendorId,
                          VendorName = v.Name,
                          VendorPhone = v.PhoneNumber,
                          Balance = b.Balance,
                          LastEntryOnUtc = b.LastEntryOnUtc
                      }).FirstOrDefaultAsync();

        return result;
    }

    private async Task<VendorLedgerEntryDto> PersistEntryAsync(VendorLedgerEntry entry)
    {
        await using var transaction = await dbContext.BeginTransactionAsync().ConfigureAwait(false);

        var inserted = await entryRepository.InsertAsync(entry).ConfigureAwait(false);
        await UpsertBalanceAsync(entry.VendorId, entry.Amount, entry.OccurredAtUtc).ConfigureAwait(false);

        await unitOfWork.CommitAsync().ConfigureAwait(false);
        await transaction.CommitAsync().ConfigureAwait(false);
        return MapToDto(inserted);
    }

    private async Task UpsertBalanceAsync(Guid vendorId, decimal delta, DateTime occurredAtUtc)
    {
        var existingId = await balanceReader.TrackingDataSource
            .Where(b => b.VendorId == vendorId)
            .Select(b => b.Id)
            .FirstOrDefaultAsync().ConfigureAwait(false);

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

        var newBalance = new VendorAccountBalance(vendorId, delta, occurredAtUtc);
        await balanceRepository.InsertAsync(newBalance).ConfigureAwait(false);
    }

    private static VendorLedgerEntryDto MapToDto(VendorLedgerEntry entry) => new()
    {
        Id = entry.Id,
        VendorId = entry.VendorId,
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
}

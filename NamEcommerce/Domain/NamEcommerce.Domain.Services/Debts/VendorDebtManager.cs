using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Domain.Services.Debts;

public sealed class VendorDebtManager(
    IRepository<VendorDebt> debtRepository,
    IEntityDataReader<VendorDebt> debtReader,
    IRepository<VendorPayment> paymentRepository,
    IEntityDataReader<VendorPayment> paymentReader,
    IRepository<VendorCreditNote> creditNoteRepository,
    IEntityDataReader<VendorCreditNote> creditNoteReader,
    IRepository<Vendor> vendorRepository,
    IEntityDataReader<PurchaseOrder> purchaseOrderReader,
    IEntityDataReader<GoodsReceipt> goodsReceiptReader,
    IVendorLedgerManager vendorLedgerManager,
    EntityCodeGenerator entityCodeGenerator) : IVendorDebtManager
{
    private Task<string> GenerateDebtCodeAsync()
    {
        var prefix = $"CN-NCC-{DateTime.UtcNow:yyMM}";
        return entityCodeGenerator.NextAsync(prefix, () => debtReader.TrackingDataSource.CountAsync(d => d.Code.StartsWith(prefix)));
    }

    private Task<string> GeneratePaymentCodeAsync()
    {
        var prefix = $"PC-NCC-{DateTime.UtcNow:yyMM}";
        return entityCodeGenerator.NextAsync(prefix, () => paymentReader.TrackingDataSource.CountAsync(p => p.Code.StartsWith(prefix)));
    }

    private Task<string> GenerateCreditNoteCodeAsync()
    {
        var prefix = $"DC-NCC-{DateTime.UtcNow:yyMM}";
        return entityCodeGenerator.NextAsync(prefix, () => creditNoteReader.TrackingDataSource.CountAsync(c => c.Code.StartsWith(prefix)));
    }

    public async Task<VendorDebtDto> CreateInitialDebtAsync(CreateInitialVendorDebtDto dto)
    {
        dto.Verify();

        var vendor = await vendorRepository.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor == null)
            throw new ArgumentException($"Vendor with id '{dto.VendorId}' is not found");

        var code = await GenerateDebtCodeAsync().ConfigureAwait(false);

        var debt = new VendorDebt(code, vendor.Id, vendor.Name, dto.TotalAmount)
        {
            VendorPhone = vendor.PhoneNumber,
            VendorAddress = vendor.Address
        };

        debt.MarkCreated();
        var inserted = await debtRepository.InsertAsync(debt).ConfigureAwait(false);

        await vendorLedgerManager.RecordChargeAsync(new RecordVendorLedgerChargeDto
        {
            VendorId = dto.VendorId,
            Amount = dto.TotalAmount,
            ReferenceType = VendorLedgerReferenceType.None,
            OccurredAtUtc = inserted.CreatedOnUtc
        }).ConfigureAwait(false);

        return inserted.ToDto();
    }

    public async Task<VendorDebtDto> CreateDebtFromPurchaseOrderAsync(CreateVendorDebtDto dto)
    {
        dto.Verify();

        var existing = await debtReader.DataSource.FirstOrDefaultAsync(d => d.PurchaseOrderId == dto.PurchaseOrderId).ConfigureAwait(false);
        if (existing != null)
            return existing.ToDto();

        var vendor = await vendorRepository.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor == null)
            throw new ArgumentException($"Vendor with id '{dto.VendorId}' is not found");

        var purchaseOrder = await purchaseOrderReader.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder == null)
            throw new ArgumentException($"PurchaseOrder with id '{dto.PurchaseOrderId}' is not found");

        var code = await GenerateDebtCodeAsync().ConfigureAwait(false);

        var debt = new VendorDebt(
            code: code,
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            purchaseOrderId: purchaseOrder.Id,
            purchaseOrderCode: purchaseOrder.Code,
            totalAmount: dto.TotalAmount,
            dueDateUtc: dto.DueDateUtc
        )
        {
            VendorPhone = vendor.PhoneNumber,
            VendorAddress = vendor.Address
        };

        debt.MarkCreated();
        var inserted = await debtRepository.InsertAsync(debt).ConfigureAwait(false);

        await vendorLedgerManager.RecordChargeAsync(new RecordVendorLedgerChargeDto
        {
            VendorId = dto.VendorId,
            Amount = dto.TotalAmount,
            ReferenceType = VendorLedgerReferenceType.None,
            OccurredAtUtc = inserted.CreatedOnUtc
        }).ConfigureAwait(false);

        return inserted.ToDto();
    }

    public async Task<VendorDebtDto> CreateDebtFromGoodsReceiptAsync(CreateVendorDebtFromGoodsReceiptDto dto)
    {
        dto.Verify();

        var existing = await debtReader.TrackingDataSource.FirstOrDefaultAsync(d => d.GoodsReceiptId == dto.GoodsReceiptId).ConfigureAwait(false);
        if (existing != null)
            return existing.ToDto();

        var vendor = await vendorRepository.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor == null)
            throw new ArgumentException($"Vendor with id '{dto.VendorId}' is not found");

        var goodsReceipt = await goodsReceiptReader.GetByIdAsync(dto.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt == null)
            throw new ArgumentException($"GoodsReceipt with id '{dto.GoodsReceiptId}' is not found");

        var code = await GenerateDebtCodeAsync().ConfigureAwait(false);

        var debt = new VendorDebt(code, vendor.Id, vendor.Name, dto.TotalAmount, dto.DueDateUtc, goodsReceipt.Id, goodsReceipt.Code)
        {
            VendorPhone = vendor.PhoneNumber,
            VendorAddress = vendor.Address
        };

        debt.MarkCreated();
        var inserted = await debtRepository.InsertAsync(debt).ConfigureAwait(false);

        await vendorLedgerManager.RecordChargeAsync(new RecordVendorLedgerChargeDto
        {
            VendorId = dto.VendorId,
            Amount = dto.TotalAmount,
            ReferenceType = VendorLedgerReferenceType.GoodsReceipt,
            ReferenceId = dto.GoodsReceiptId,
            OccurredAtUtc = inserted.CreatedOnUtc
        }).ConfigureAwait(false);

        return inserted.ToDto();
    }

    public async Task<VendorPaymentDto> RecordPaymentAsync(CreateVendorPaymentDto dto)
    {
        dto.Verify();

        var vendor = await vendorRepository.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor == null)
            throw new ArgumentException($"Vendor with id '{dto.VendorId}' is not found");

        var code = await GeneratePaymentCodeAsync().ConfigureAwait(false);

        var payment = new VendorPayment(code, vendor.Id, vendor.Name, dto.Amount,
            dto.PaymentMethod, dto.PaymentType, dto.PaidOnUtc, dto.RecordedByUserId, dto.Note)
        {
            VendorDebtId = dto.VendorDebtId,
            PurchaseOrderId = dto.PurchaseOrderId,
            BankAccountId = dto.PaymentMethod == PaymentMethod.BankTransfer ? dto.BankAccountId : null
        };

        payment.MarkCreated();
        var inserted = await paymentRepository.InsertAsync(payment).ConfigureAwait(false);

        await vendorLedgerManager.RecordPaymentAsync(new RecordVendorLedgerPaymentDto
        {
            VendorId = dto.VendorId,
            Amount = dto.Amount,
            ReferenceId = inserted.Id,
            ReferenceCode = inserted.Code,
            OccurredAtUtc = inserted.PaidOnUtc,
            CreatedByUserId = dto.RecordedByUserId
        }).ConfigureAwait(false);

        return inserted.ToDto();
    }

    public async Task<IList<VendorPaymentDto>> RecordFlexiblePaymentForVendorAsync(CreateVendorPaymentDto dto)
    {
        var payment = await RecordPaymentAsync(dto).ConfigureAwait(false);
        return [payment];
    }

    public async Task<VendorPaymentDto> RecordAdvancePaymentAsync(CreateVendorPaymentDto dto)
    {
        dto.Verify();

        var vendor = await vendorRepository.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor == null)
            throw new ArgumentException($"Vendor with id '{dto.VendorId}' is not found");

        var code = await GeneratePaymentCodeAsync().ConfigureAwait(false);

        var payment = new VendorPayment(code, vendor.Id, vendor.Name,
            dto.Amount, dto.PaymentMethod, PaymentType.AdvancePayment,
            dto.PaidOnUtc, dto.RecordedByUserId, dto.Note)
        {
            BankAccountId = dto.PaymentMethod == PaymentMethod.BankTransfer ? dto.BankAccountId : null
        };

        payment.MarkCreated();
        var inserted = await paymentRepository.InsertAsync(payment).ConfigureAwait(false);

        await vendorLedgerManager.RecordPaymentAsync(new RecordVendorLedgerPaymentDto
        {
            VendorId = dto.VendorId,
            Amount = dto.Amount,
            ReferenceId = inserted.Id,
            ReferenceCode = inserted.Code,
            OccurredAtUtc = inserted.PaidOnUtc,
            CreatedByUserId = dto.RecordedByUserId
        }).ConfigureAwait(false);

        return inserted.ToDto();
    }

    public async Task<VendorDebtDto?> GetDebtByIdAsync(Guid id)
    {
        var debt = await debtRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (debt == null) return null;

        var payments = await paymentReader.DataSource
            .Where(p => p.VendorDebtId == id)
            .OrderBy(p => p.PaidOnUtc)
            .ToListAsync().ConfigureAwait(false);

        var allocations = await GetCreditNoteAllocationsByDebtIdAsync(id).ConfigureAwait(false);

        var dto = debt.ToDto();
        return dto with
        {
            Payments = payments.Select(p => p.ToDto()).ToList(),
            CreditNoteAllocations = allocations
        };
    }

    public async Task<VendorDebtDto?> GetDebtByGoodsReceiptIdAsync(Guid goodsReceiptId)
    {
        var debt = await debtReader.DataSource
            .FirstOrDefaultAsync(d => d.GoodsReceiptId == goodsReceiptId)
            .ConfigureAwait(false);
        if (debt == null) return null;

        var payments = await paymentReader.DataSource
            .Where(p => p.VendorDebtId == debt.Id)
            .OrderBy(p => p.PaidOnUtc)
            .ToListAsync().ConfigureAwait(false);

        var allocations = await GetCreditNoteAllocationsByDebtIdAsync(debt.Id).ConfigureAwait(false);

        var dto = debt.ToDto();
        return dto with
        {
            Payments = payments.Select(p => p.ToDto()).ToList(),
            CreditNoteAllocations = allocations
        };
    }

    public async Task DeleteDebtFromGoodsReceiptAsync(Guid goodsReceiptId)
    {
        var debtId = await debtReader.DataSource
            .Where(d => d.GoodsReceiptId == goodsReceiptId)
            .Select(d => d.Id)
            .FirstOrDefaultAsync().ConfigureAwait(false);
        if (debtId == Guid.Empty) return;

        var debt = await debtRepository.GetByIdAsync(debtId).ConfigureAwait(false);
        if (debt == null) return;

        await debtRepository.DeleteAsync(debt).ConfigureAwait(false);
    }

    public async Task<VendorCreditNoteDto> ApplyCreditNoteFromVendorReturnAsync(Guid vendorId, Guid returnId,
        string returnCode, Guid? sourceGoodsReceiptId, Guid? sourcePurchaseOrderId, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Credit note amount must be positive", nameof(amount));

        var existing = await creditNoteReader.TrackingDataSource
            .FirstOrDefaultAsync(c => c.SourceReturnId == returnId && c.Status != CreditNoteStatus.Cancelled)
            .ConfigureAwait(false);
        if (existing is not null)
            return existing.ToDto();

        var vendor = await vendorRepository.GetByIdAsync(vendorId).ConfigureAwait(false);
        if (vendor == null)
            throw new ArgumentException($"Vendor with id '{vendorId}' is not found");

        var code = await GenerateCreditNoteCodeAsync().ConfigureAwait(false);
        var creditNote = new VendorCreditNote(code, vendor.Id, vendor.Name,
            returnId, returnCode, sourceGoodsReceiptId, sourcePurchaseOrderId, amount);

        var inserted = await creditNoteRepository.InsertAsync(creditNote).ConfigureAwait(false);

        return inserted.ToDto();
    }

    public async Task ReverseCreditNoteFromVendorReturnAsync(Guid returnId, string reason)
    {
        var creditNoteId = await creditNoteReader.DataSource
            .Where(c => c.SourceReturnId == returnId && c.Status != CreditNoteStatus.Cancelled)
            .Select(c => c.Id)
            .FirstOrDefaultAsync().ConfigureAwait(false);
        if (creditNoteId == Guid.Empty) return;

        var creditNote = await creditNoteRepository.GetByIdAsync(creditNoteId).ConfigureAwait(false);
        if (creditNote is null) return;

        creditNote.Cancel();
        await creditNoteRepository.UpdateAsync(creditNote).ConfigureAwait(false);

        await vendorLedgerManager.RecordCorrectionAsync(new RecordVendorCorrectionDto
        {
            VendorId = creditNote.VendorId,
            Amount = creditNote.Amount,
            Note = reason,
            OccurredAtUtc = DateTime.UtcNow
        }).ConfigureAwait(false);
    }

    public async Task<VendorPaymentDto?> GetPaymentByIdAsync(Guid paymentId)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId).ConfigureAwait(false);
        return payment == null ? null : payment.ToDto();
    }

    public async Task<VendorDebtSummaryDto?> GetVendorDebtSummaryAsync(Guid vendorId)
    {
        var debts = await debtReader.DataSource
            .Where(d => d.VendorId == vendorId)
            .ToListAsync().ConfigureAwait(false);

        if (debts.Count == 0) return null;

        var vendorName = debts[0].VendorName;

        return new VendorDebtSummaryDto
        {
            VendorId = vendorId,
            VendorName = vendorName,
            TotalDebtAmount = debts.Sum(d => d.TotalAmount),
            TotalPaidAmount = debts.Sum(d => d.PaidAmount),
            TotalRemainingAmount = debts.Sum(d => d.RemainingAmount),
            DebtCount = debts.Count
        };
    }

    public async Task<IPagedDataDto<VendorDebtSummaryDto>> GetVendorsWithDebtsAsync(
        string? keywords = null, int pageIndex = 0, int pageSize = 15)
    {
        var allDebts = await debtReader.DataSource.ToListAsync().ConfigureAwait(false);

        var groups = allDebts
            .GroupBy(d => d.VendorId)
            .Select(g => new
            {
                VendorId = g.Key,
                g.First().VendorName,
                g.First().VendorPhone,
                g.First().VendorAddress,
                TotalDebtAmount = g.Sum(d => d.TotalAmount),
                TotalPaidAmount = g.Sum(d => d.PaidAmount),
                TotalRemainingAmount = g.Sum(d => d.RemainingAmount),
                DebtCount = g.Count()
            })
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(keywords))
            groups = groups.Where(g => g.VendorName.Contains(keywords, StringComparison.OrdinalIgnoreCase));

        var sorted = groups.OrderBy(g => g.VendorName).ToList();
        var total = sorted.Count;
        var page = sorted.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        var results = new List<VendorDebtSummaryDto>();
        foreach (var item in page)
        {
            var advanceBalance = await paymentReader.DataSource
                .Where(p => p.VendorId == item.VendorId
                         && p.PaymentType == PaymentType.AdvancePayment
                         && !p.IsApplied)
                .SumAsync(p => p.Amount).ConfigureAwait(false);

            results.Add(new VendorDebtSummaryDto
            {
                VendorId = item.VendorId,
                VendorName = item.VendorName,
                VendorPhone = item.VendorPhone,
                VendorAddress = item.VendorAddress,
                TotalDebtAmount = item.TotalDebtAmount,
                TotalPaidAmount = item.TotalPaidAmount,
                TotalRemainingAmount = item.TotalRemainingAmount,
                AdvanceBalance = advanceBalance,
                DebtCount = item.DebtCount
            });
        }

        return PagedDataDto.Create(results, pageIndex, pageSize, total);
    }

    public async Task<VendorDebtsByVendorDto?> GetDebtsByVendorIdAsync(Guid vendorId)
    {
        var debts = await debtReader.DataSource
            .Where(d => d.VendorId == vendorId)
            .OrderByDescending(d => d.CreatedOnUtc)
            .ToListAsync().ConfigureAwait(false);

        if (debts.Count == 0) return null;

        var debtDtos = new List<VendorDebtDto>();
        foreach (var debt in debts)
        {
            var payments = await paymentReader.DataSource
                .Where(p => p.VendorDebtId == debt.Id)
                .OrderBy(p => p.PaidOnUtc)
                .ToListAsync().ConfigureAwait(false);
            var allocations = await GetCreditNoteAllocationsByDebtIdAsync(debt.Id).ConfigureAwait(false);
            debtDtos.Add(debt.ToDto() with
            {
                Payments = payments.Select(p => p.ToDto()).ToList(),
                CreditNoteAllocations = allocations
            });
        }

        var advances = await paymentReader.DataSource
            .Where(p => p.VendorId == vendorId && p.PaymentType == PaymentType.AdvancePayment && !p.IsApplied)
            .OrderByDescending(p => p.PaidOnUtc)
            .ToListAsync().ConfigureAwait(false);

        var recentPayments = await paymentReader.DataSource
            .Where(p => p.VendorId == vendorId)
            .OrderByDescending(p => p.PaidOnUtc)
            .Take(20)
            .ToListAsync().ConfigureAwait(false);

        var advanceBalance = advances.Sum(p => p.Amount);
        var unappliedCreditNotes = await creditNoteReader.DataSource
            .Where(c => c.VendorId == vendorId
                && c.Status != CreditNoteStatus.Cancelled
                && c.RemainingAmount > 0)
            .OrderByDescending(c => c.CreatedOnUtc)
            .ToListAsync().ConfigureAwait(false);

        return new VendorDebtsByVendorDto
        {
            VendorId = vendorId,
            VendorName = debts[0].VendorName,
            TotalDebtAmount = debts.Sum(d => d.TotalAmount),
            TotalPaidAmount = debts.Sum(d => d.PaidAmount),
            TotalRemainingAmount = debts.Sum(d => d.RemainingAmount),
            AdvanceBalance = advanceBalance,
            Debts = debtDtos,
            AdvancePayments = advances.Select(p => p.ToDto()).ToList(),
            RecentPayments = recentPayments.Select(p => p.ToDto()).ToList(),
            UnappliedCreditNoteBalance = unappliedCreditNotes.Sum(c => c.RemainingAmount),
            UnappliedCreditNotes = unappliedCreditNotes.Select(c => c.ToDto()).ToList()
        };
    }

    public async Task<IPagedDataDto<VendorDebtDto>> GetDebtsAsync(
        Guid? vendorId = null, string? keywords = null, int pageIndex = 0, int pageSize = 15)
    {
        var query = debtReader.DataSource;
        if (vendorId.HasValue)
            query = query.Where(d => d.VendorId == vendorId.Value);
        if (!string.IsNullOrWhiteSpace(keywords))
            query = query.Where(d => d.Code.Contains(keywords)
                || d.VendorName.Contains(keywords)
                || (d.PurchaseOrderCode != null && d.PurchaseOrderCode.Contains(keywords)));

        query = query.OrderByDescending(d => d.CreatedOnUtc);

        var total = await query.CountAsync().ConfigureAwait(false);
        var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync().ConfigureAwait(false);

        return PagedDataDto.Create(items.Select(d => d.ToDto()).ToList(), pageIndex, pageSize, total);
    }

    public async Task<IPagedDataDto<VendorPaymentDto>> GetPaymentsAsync(
        Guid? vendorId = null, int pageIndex = 0, int pageSize = 15)
    {
        var query = paymentReader.DataSource;
        if (vendorId.HasValue)
            query = query.Where(p => p.VendorId == vendorId.Value);

        query = query.OrderByDescending(p => p.CreatedOnUtc);

        var total = await query.CountAsync().ConfigureAwait(false);
        var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync().ConfigureAwait(false);

        return PagedDataDto.Create(items.Select(p => p.ToDto()).ToList(), pageIndex, pageSize, total);
    }

    private async Task<IList<VendorCreditNoteAllocationDto>> GetCreditNoteAllocationsByDebtIdAsync(Guid debtId)
        => (await creditNoteReader.DataSource
            .SelectMany(c => c.Allocations)
            .Where(a => a.VendorDebtId == debtId)
            .OrderBy(a => a.AppliedOnUtc)
            .ToListAsync().ConfigureAwait(false))
            .Select(a => a.ToDto())
            .ToList();
}

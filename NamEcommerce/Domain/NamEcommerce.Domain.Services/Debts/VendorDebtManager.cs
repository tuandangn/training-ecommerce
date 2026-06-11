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
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Domain.Services.Debts;

public sealed class VendorDebtManager(
    IRepository<VendorDebt> debtRepository,
    IEntityDataReader<VendorDebt> debtReader,
    IRepository<VendorPayment> paymentRepository,
    IEntityDataReader<VendorPayment> paymentReader,
    IRepository<VendorCreditNote> creditNoteRepository,
    IEntityDataReader<VendorCreditNote> creditNoteReader,
    IEntityDataReader<Vendor> vendorReader,
    IEntityDataReader<PurchaseOrder> purchaseOrderReader,
    IEntityDataReader<GoodsReceipt> goodsReceiptReader,
    IVendorLedgerManager vendorLedgerManager,
    EntityCodeGenerator entityCodeGenerator) : IVendorDebtManager
{
    private Task<string> GenerateDebtCodeAsync()
    {
        var prefix = $"CN-NCC-{DateTime.UtcNow:yyMM}";
        return Task.FromResult(entityCodeGenerator.Next(prefix, () => debtReader.SecuredDataSource.Count(d => d.Code.StartsWith(prefix))));
    }

    private Task<string> GeneratePaymentCodeAsync()
    {
        var prefix = $"PC-NCC-{DateTime.UtcNow:yyMM}";
        return Task.FromResult(entityCodeGenerator.Next(prefix, () => paymentReader.SecuredDataSource.Count(p => p.Code.StartsWith(prefix))));
    }

    private Task<string> GenerateCreditNoteCodeAsync()
    {
        var prefix = $"DC-NCC-{DateTime.UtcNow:yyMM}";
        return Task.FromResult(entityCodeGenerator.Next(prefix, () => creditNoteReader.SecuredDataSource.Count(c => c.Code.StartsWith(prefix))));
    }

    public async Task<VendorDebtDto> CreateInitialDebtAsync(CreateInitialVendorDebtDto dto)
    {
        dto.Verify();

        var vendor = await vendorReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor == null)
            throw new ArgumentException($"Vendor with id '{dto.VendorId}' is not found");

        var code = await GenerateDebtCodeAsync().ConfigureAwait(false);

        var debt = new VendorDebt(
            code: code,
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            totalAmount: dto.TotalAmount
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

    public async Task<VendorDebtDto> CreateDebtFromPurchaseOrderAsync(CreateVendorDebtDto dto)
    {
        dto.Verify();

        // Idempotency: trả về existing nếu đã có debt cho PO này
        var existing = debtReader.DataSource.FirstOrDefault(d => d.PurchaseOrderId == dto.PurchaseOrderId);
        if (existing != null)
            return existing.ToDto();

        var vendor = await vendorReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
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

        // Idempotency: trả về existing nếu đã có debt cho GoodsReceipt này
        var existing = debtReader.DataSource.FirstOrDefault(d => d.GoodsReceiptId == dto.GoodsReceiptId);
        if (existing != null)
            return existing.ToDto();

        var vendor = await vendorReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor == null)
            throw new ArgumentException($"Vendor with id '{dto.VendorId}' is not found");

        var goodsReceipt = await goodsReceiptReader.GetByIdAsync(dto.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt == null)
            throw new ArgumentException($"GoodsReceipt with id '{dto.GoodsReceiptId}' is not found");

        var code = await GenerateDebtCodeAsync().ConfigureAwait(false);

        var debt = new VendorDebt(
            code: code,
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            goodsReceiptId: goodsReceipt.Id,
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
            ReferenceType = VendorLedgerReferenceType.GoodsReceipt,
            ReferenceId = dto.GoodsReceiptId,
            OccurredAtUtc = inserted.CreatedOnUtc
        }).ConfigureAwait(false);

        return inserted.ToDto();
    }

    public async Task<VendorPaymentDto> RecordPaymentAsync(CreateVendorPaymentDto dto)
    {
        dto.Verify();

        var vendor = await vendorReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor == null)
            throw new ArgumentException($"Vendor with id '{dto.VendorId}' is not found");

        var code = await GeneratePaymentCodeAsync().ConfigureAwait(false);

        var payment = new VendorPayment(
            code: code,
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            amount: dto.Amount,
            paymentMethod: dto.PaymentMethod,
            paymentType: dto.PaymentType,
            paidOnUtc: dto.PaidOnUtc,
            recordedByUserId: dto.RecordedByUserId,
            note: dto.Note
        )
        {
            VendorDebtId = dto.VendorDebtId,
            PurchaseOrderId = dto.PurchaseOrderId
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

        var vendor = await vendorReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor == null)
            throw new ArgumentException($"Vendor with id '{dto.VendorId}' is not found");

        var code = await GeneratePaymentCodeAsync().ConfigureAwait(false);

        var payment = new VendorPayment(
            code: code,
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            amount: dto.Amount,
            paymentMethod: dto.PaymentMethod,
            paymentType: PaymentType.AdvancePayment,
            paidOnUtc: dto.PaidOnUtc,
            recordedByUserId: dto.RecordedByUserId,
            note: dto.Note
        );

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
        var debt = await debtReader.GetByIdAsync(id).ConfigureAwait(false);
        if (debt == null) return null;

        var payments = paymentReader.DataSource
            .Where(p => p.VendorDebtId == id)
            .OrderBy(p => p.PaidOnUtc)
            .ToList();

        var allocations = GetCreditNoteAllocationsByDebtId(id);

        var dto = debt.ToDto();
        return dto with
        {
            Payments = payments.Select(p => p.ToDto()).ToList(),
            CreditNoteAllocations = allocations
        };
    }

    public async Task<VendorDebtDto?> GetDebtByGoodsReceiptIdAsync(Guid goodsReceiptId)
    {
        // Idempotency của CreateDebtFromGoodsReceiptAsync đảm bảo chỉ có tối đa 1 debt cho mỗi GoodsReceipt.
        var debt = debtReader.DataSource
            .FirstOrDefault(d => d.GoodsReceiptId == goodsReceiptId);
        if (debt == null) return null;

        var payments = paymentReader.DataSource
            .Where(p => p.VendorDebtId == debt.Id)
            .OrderBy(p => p.PaidOnUtc)
            .ToList();

        var allocations = GetCreditNoteAllocationsByDebtId(debt.Id);

        var dto = debt.ToDto();
        return dto with
        {
            Payments = payments.Select(p => p.ToDto()).ToList(),
            CreditNoteAllocations = allocations
        };
    }

    public async Task DeleteDebtFromGoodsReceiptAsync(Guid goodsReceiptId)
    {
        var debtId = debtReader.DataSource
            .Where(d => d.GoodsReceiptId == goodsReceiptId)
            .Select(d => d.Id)
            .FirstOrDefault();
        if (debtId == Guid.Empty) return;

        var debt = await debtRepository.GetByIdAsync(debtId).ConfigureAwait(false);
        if (debt == null) return;

        await debtRepository.DeleteAsync(debt).ConfigureAwait(false);
    }

    public async Task<VendorCreditNoteDto> ApplyCreditNoteFromVendorReturnAsync(
        Guid vendorId,
        Guid returnId,
        string returnCode,
        Guid? sourceGoodsReceiptId,
        Guid? sourcePurchaseOrderId,
        decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Credit note amount must be positive", nameof(amount));

        var existing = creditNoteReader.DataSource
            .FirstOrDefault(c => c.SourceReturnId == returnId && c.Status != CreditNoteStatus.Cancelled);
        if (existing is not null)
            return existing.ToDto();

        var vendor = await vendorReader.GetByIdAsync(vendorId).ConfigureAwait(false);
        if (vendor == null)
            throw new ArgumentException($"Vendor with id '{vendorId}' is not found");

        var code = await GenerateCreditNoteCodeAsync().ConfigureAwait(false);
        var creditNote = new VendorCreditNote(
            code,
            vendor.Id,
            vendor.Name,
            returnId,
            returnCode,
            sourceGoodsReceiptId,
            sourcePurchaseOrderId,
            amount);

        var inserted = await creditNoteRepository.InsertAsync(creditNote).ConfigureAwait(false);

        await vendorLedgerManager.RecordReturnCreditAsync(new RecordVendorLedgerReturnCreditDto
        {
            VendorId = vendorId,
            Amount = amount,
            ReferenceId = returnId,
            ReferenceCode = returnCode,
            OccurredAtUtc = inserted.CreatedOnUtc
        }).ConfigureAwait(false);

        return inserted.ToDto();
    }

    public async Task ConsumeCreditNoteByRefundAsync(Guid vendorReturnId, decimal refundAmount)
    {
        var creditNote = creditNoteReader.DataSource
            .FirstOrDefault(c => c.SourceReturnId == vendorReturnId
                              && c.Status != CreditNoteStatus.Cancelled
                              && c.RemainingAmount > 0);
        if (creditNote is null) return;

        var tracked = await creditNoteRepository.GetByIdAsync(creditNote.Id).ConfigureAwait(false);
        if (tracked is null) return;

        tracked.ConsumeByRefund(refundAmount);
        await creditNoteRepository.UpdateAsync(tracked).ConfigureAwait(false);
    }

    public async Task ReverseCreditNoteFromVendorReturnAsync(Guid returnId, string reason)
    {
        var creditNoteId = creditNoteReader.DataSource
            .Where(c => c.SourceReturnId == returnId && c.Status != CreditNoteStatus.Cancelled)
            .Select(c => c.Id)
            .FirstOrDefault();
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
        var payment = await paymentReader.GetByIdAsync(paymentId).ConfigureAwait(false);
        return payment == null ? null : payment.ToDto();
    }

    public async Task<VendorDebtSummaryDto?> GetVendorDebtSummaryAsync(Guid vendorId)
    {
        var debts = debtReader.DataSource
            .Where(d => d.VendorId == vendorId)
            .ToList();

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
        var allDebts = debtReader.DataSource.ToList();

        var groups = allDebts
            .GroupBy(d => d.VendorId)
            .Select(g => new
            {
                VendorId = g.Key,
                VendorName = g.First().VendorName,
                VendorPhone = g.First().VendorPhone,
                VendorAddress = g.First().VendorAddress,
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
            var advanceBalance = paymentReader.DataSource
                .Where(p => p.VendorId == item.VendorId
                         && p.PaymentType == PaymentType.AdvancePayment
                         && !p.IsApplied)
                .Sum(p => p.Amount);

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
        var debts = debtReader.DataSource
            .Where(d => d.VendorId == vendorId)
            .OrderByDescending(d => d.CreatedOnUtc)
            .ToList();

        if (debts.Count == 0) return null;

        // Load payments gắn với từng debt
        var debtDtos = new List<VendorDebtDto>();
        foreach (var debt in debts)
        {
            var payments = paymentReader.DataSource
                .Where(p => p.VendorDebtId == debt.Id)
                .OrderBy(p => p.PaidOnUtc)
                .ToList();
            var allocations = GetCreditNoteAllocationsByDebtId(debt.Id);
            debtDtos.Add(debt.ToDto() with
            {
                Payments = payments.Select(p => p.ToDto()).ToList(),
                CreditNoteAllocations = allocations
            });
        }

        // Tiền ứng trước chưa áp dụng
        var advances = paymentReader.DataSource
            .Where(p => p.VendorId == vendorId && p.PaymentType == PaymentType.AdvancePayment && !p.IsApplied)
            .OrderByDescending(p => p.PaidOnUtc)
            .ToList();

        // Lịch sử 20 giao dịch gần nhất
        var recentPayments = paymentReader.DataSource
            .Where(p => p.VendorId == vendorId)
            .OrderByDescending(p => p.PaidOnUtc)
            .Take(20)
            .ToList();

        var advanceBalance = advances.Sum(p => p.Amount);
        var unappliedCreditNotes = creditNoteReader.DataSource
            .Where(c => c.VendorId == vendorId
                && c.Status != CreditNoteStatus.Cancelled
                && c.RemainingAmount > 0)
            .OrderByDescending(c => c.CreatedOnUtc)
            .ToList();

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

        var total = query.Count();
        var items = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        return PagedDataDto.Create(items.Select(d => d.ToDto()).ToList(), pageIndex, pageSize, total);
    }

    public async Task<IPagedDataDto<VendorPaymentDto>> GetPaymentsAsync(
        Guid? vendorId = null, int pageIndex = 0, int pageSize = 15)
    {
        var query = paymentReader.DataSource;
        if (vendorId.HasValue)
            query = query.Where(p => p.VendorId == vendorId.Value);

        query = query.OrderByDescending(p => p.CreatedOnUtc);

        var total = query.Count();
        var items = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        return PagedDataDto.Create(items.Select(p => p.ToDto()).ToList(), pageIndex, pageSize, total);
    }

    private IList<VendorCreditNoteAllocationDto> GetCreditNoteAllocationsByDebtId(Guid debtId)
        => creditNoteReader.DataSource
            .SelectMany(c => c.Allocations)
            .Where(a => a.VendorDebtId == debtId)
            .OrderBy(a => a.AppliedOnUtc)
            .Select(a => a.ToDto())
            .ToList();
}

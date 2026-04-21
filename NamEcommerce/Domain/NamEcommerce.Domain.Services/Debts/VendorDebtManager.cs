using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Debts;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Domain.Services.Debts;

public sealed class VendorDebtManager(
    IRepository<VendorDebt> debtRepository,
    IEntityDataReader<VendorDebt> debtReader,
    IRepository<VendorPayment> paymentRepository,
    IEntityDataReader<VendorPayment> paymentReader,
    IEntityDataReader<Vendor> vendorReader,
    IEntityDataReader<PurchaseOrder> purchaseOrderReader,
    IEventPublisher eventPublisher) : IVendorDebtManager
{
    private async Task<string> GenerateDebtCodeAsync()
    {
        var datePrefix = $"CNNCC-{DateTime.UtcNow:yyyyMMdd}";
        var count = debtReader.DataSource.Count(d => d.Code.StartsWith(datePrefix));
        return $"{datePrefix}-{(count + 1):D3}";
    }

    private async Task<string> GeneratePaymentCodeAsync()
    {
        var datePrefix = $"PCNCC-{DateTime.UtcNow:yyyyMMdd}";
        var count = paymentReader.DataSource.Count(p => p.Code.StartsWith(datePrefix));
        return $"{datePrefix}-{(count + 1):D3}";
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
            dueDateUtc: dto.DueDateUtc,
            createdByUserId: dto.CreatedByUserId
        )
        {
            VendorPhone = vendor.PhoneNumber,
            VendorAddress = vendor.Address
        };

        // Auto-apply AdvancePayments chưa dùng của NCC này
        var advancePayments = paymentReader.DataSource
            .Where(p => p.VendorId == vendor.Id
                     && p.PaymentType == PaymentType.AdvancePayment
                     && !p.IsApplied)
            .OrderBy(p => p.PaidOnUtc)
            .ToList();

        foreach (var advance in advancePayments)
        {
            if (debt.RemainingAmount <= 0) break;

            var applyAmount = Math.Min(advance.Amount, debt.RemainingAmount);
            debt.ApplyPayment(applyAmount);
            advance.MarkAsApplied();
            advance.VendorDebtId = debt.Id;
            advance.PurchaseOrderId = purchaseOrder.Id;
            advance.PurchaseOrderCode = purchaseOrder.Code;
            await paymentRepository.UpdateAsync(advance).ConfigureAwait(false);
        }

        var inserted = await debtRepository.InsertAsync(debt).ConfigureAwait(false);
        await eventPublisher.EntityCreated(inserted).ConfigureAwait(false);
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

        // Nếu gắn với debt cụ thể, áp dụng thanh toán vào debt đó
        if (dto.VendorDebtId.HasValue)
        {
            var debt = await debtRepository.GetByIdAsync(dto.VendorDebtId.Value).ConfigureAwait(false);
            if (debt != null)
            {
                if (payment.Amount > debt.RemainingAmount)
                    throw new VendorPaymentExceedsRemainingException(payment.Amount, debt.RemainingAmount);

                debt.ApplyPayment(payment.Amount);
                payment.MarkAsApplied();
                payment.PurchaseOrderId = debt.PurchaseOrderId;
                payment.PurchaseOrderCode = debt.PurchaseOrderCode;
                await debtRepository.UpdateAsync(debt).ConfigureAwait(false);
                await eventPublisher.EntityUpdated(debt).ConfigureAwait(false);
            }
        }

        var inserted = await paymentRepository.InsertAsync(payment).ConfigureAwait(false);
        await eventPublisher.EntityCreated(inserted).ConfigureAwait(false);
        return inserted.ToDto();
    }

    public async Task<IList<VendorPaymentDto>> RecordFlexiblePaymentForVendorAsync(CreateVendorPaymentDto dto)
    {
        dto.Verify();

        var vendor = await vendorReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor == null)
            throw new ArgumentException($"Vendor with id '{dto.VendorId}' is not found");

        // Lấy tất cả debts chưa trả hết, sắp xếp cũ nhất trước (FIFO)
        var pendingDebts = debtReader.DataSource
            .Where(d => d.VendorId == dto.VendorId && d.RemainingAmount > 0)
            .OrderBy(d => d.CreatedOnUtc)
            .ToList();

        var results = new List<VendorPaymentDto>();
        var remaining = dto.Amount;

        foreach (var debt in pendingDebts)
        {
            if (remaining <= 0) break;

            var applyAmount = Math.Min(remaining, debt.RemainingAmount);
            var code = await GeneratePaymentCodeAsync().ConfigureAwait(false);

            var payment = new VendorPayment(
                code: code,
                vendorId: vendor.Id,
                vendorName: vendor.Name,
                amount: applyAmount,
                paymentMethod: dto.PaymentMethod,
                paymentType: PaymentType.VendorDebtPayment,
                paidOnUtc: dto.PaidOnUtc,
                recordedByUserId: dto.RecordedByUserId,
                note: dto.Note
            )
            {
                VendorDebtId = debt.Id,
                PurchaseOrderId = debt.PurchaseOrderId,
                PurchaseOrderCode = debt.PurchaseOrderCode
            };

            debt.ApplyPayment(applyAmount);
            payment.MarkAsApplied();

            await debtRepository.UpdateAsync(debt).ConfigureAwait(false);
            await eventPublisher.EntityUpdated(debt).ConfigureAwait(false);
            var inserted = await paymentRepository.InsertAsync(payment).ConfigureAwait(false);
            results.Add(inserted.ToDto());

            remaining -= applyAmount;
        }

        // Nếu còn dư tiền sau khi trả hết nợ → lưu làm AdvancePayment
        if (remaining > 0)
        {
            var code = await GeneratePaymentCodeAsync().ConfigureAwait(false);
            var overpayment = new VendorPayment(
                code: code,
                vendorId: vendor.Id,
                vendorName: vendor.Name,
                amount: remaining,
                paymentMethod: dto.PaymentMethod,
                paymentType: PaymentType.AdvancePayment,
                paidOnUtc: dto.PaidOnUtc,
                recordedByUserId: dto.RecordedByUserId,
                note: string.IsNullOrEmpty(dto.Note) ? "Tiền dư sau khi thanh toán nợ NCC" : dto.Note
            );
            var inserted = await paymentRepository.InsertAsync(overpayment).ConfigureAwait(false);
            results.Add(inserted.ToDto());
        }

        return results;
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

        var inserted = await paymentRepository.InsertAsync(payment).ConfigureAwait(false);
        await eventPublisher.EntityCreated(inserted).ConfigureAwait(false);
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

        var dto = debt.ToDto();
        return dto with { Payments = payments.Select(p => p.ToDto()).ToList() };
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
            debtDtos.Add(debt.ToDto() with { Payments = payments.Select(p => p.ToDto()).ToList() });
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
            RecentPayments = recentPayments.Select(p => p.ToDto()).ToList()
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
                || d.PurchaseOrderCode.Contains(keywords));

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
}

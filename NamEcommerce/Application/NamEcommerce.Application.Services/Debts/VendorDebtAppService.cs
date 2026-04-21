using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Application.Services.Debts;

public sealed class VendorDebtAppService(IVendorDebtManager debtManager) : IVendorDebtAppService
{
    private readonly IVendorDebtManager _debtManager = debtManager;

    // ── Write operations ─────────────────────────────────────────────────────

    public async Task<CreateVendorDebtResultAppDto> CreateDebtFromPurchaseOrderAsync(CreateVendorDebtAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new CreateVendorDebtResultAppDto { Success = false, ErrorMessage = errorMessage };

        try
        {
            var domainDto = MapToDomainDto(dto);
            var result = await _debtManager.CreateDebtFromPurchaseOrderAsync(domainDto).ConfigureAwait(false);
            return new CreateVendorDebtResultAppDto
            {
                Success = true,
                CreatedId = result.Id,
                Debt = result.ToDto()
            };
        }
        catch (Exception ex)
        {
            return new CreateVendorDebtResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<RecordVendorPaymentResultAppDto> RecordPaymentAsync(CreateVendorPaymentAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new RecordVendorPaymentResultAppDto { Success = false, ErrorMessage = errorMessage };

        try
        {
            var domainDto = MapToDomainDto(dto);
            var result = await _debtManager.RecordPaymentAsync(domainDto).ConfigureAwait(false);
            return new RecordVendorPaymentResultAppDto
            {
                Success = true,
                CreatedId = result.Id,
                Payment = result.ToDto()
            };
        }
        catch (Exception ex)
        {
            return new RecordVendorPaymentResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<RecordVendorPaymentResultAppDto> RecordFlexiblePaymentForVendorAsync(CreateVendorPaymentAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new RecordVendorPaymentResultAppDto { Success = false, ErrorMessage = errorMessage };

        try
        {
            var domainDto = MapToDomainDto(dto);
            var results = await _debtManager.RecordFlexiblePaymentForVendorAsync(domainDto).ConfigureAwait(false);
            return new RecordVendorPaymentResultAppDto
            {
                Success = true,
                Payments = results.Select(p => p.ToDto()).ToList()
            };
        }
        catch (Exception ex)
        {
            return new RecordVendorPaymentResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<RecordVendorPaymentResultAppDto> RecordAdvancePaymentAsync(CreateVendorPaymentAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new RecordVendorPaymentResultAppDto { Success = false, ErrorMessage = errorMessage };

        try
        {
            var domainDto = MapToDomainDto(dto);
            var result = await _debtManager.RecordAdvancePaymentAsync(domainDto).ConfigureAwait(false);
            return new RecordVendorPaymentResultAppDto
            {
                Success = true,
                CreatedId = result.Id,
                Payment = result.ToDto()
            };
        }
        catch (Exception ex)
        {
            return new RecordVendorPaymentResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    // ── Read operations ──────────────────────────────────────────────────────

    public async Task<VendorDebtAppDto?> GetDebtByIdAsync(Guid id)
    {
        var result = await _debtManager.GetDebtByIdAsync(id).ConfigureAwait(false);
        return result?.ToDto();
    }

    public async Task<VendorPaymentAppDto?> GetPaymentByIdAsync(Guid paymentId)
    {
        var result = await _debtManager.GetPaymentByIdAsync(paymentId).ConfigureAwait(false);
        return result?.ToDto();
    }

    public async Task<VendorDebtSummaryAppDto?> GetVendorDebtSummaryAsync(Guid vendorId)
    {
        var result = await _debtManager.GetVendorDebtSummaryAsync(vendorId).ConfigureAwait(false);
        if (result == null) return null;
        return new VendorDebtSummaryAppDto
        {
            VendorId = result.VendorId,
            VendorName = result.VendorName,
            VendorPhone = result.VendorPhone,
            VendorAddress = result.VendorAddress,
            TotalDebtAmount = result.TotalDebtAmount,
            TotalPaidAmount = result.TotalPaidAmount,
            TotalRemainingAmount = result.TotalRemainingAmount,
            AdvanceBalance = result.AdvanceBalance,
            DebtCount = result.DebtCount
        };
    }

    public async Task<PagedDataAppDto<VendorDebtSummaryAppDto>> GetVendorsWithDebtsAsync(
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15)
    {
        var paged = await _debtManager.GetVendorsWithDebtsAsync(keywords, pageIndex, pageSize).ConfigureAwait(false);
        var mappedItems = paged.Items.Select(s => new VendorDebtSummaryAppDto
        {
            VendorId = s.VendorId,
            VendorName = s.VendorName,
            VendorPhone = s.VendorPhone,
            VendorAddress = s.VendorAddress,
            TotalDebtAmount = s.TotalDebtAmount,
            TotalPaidAmount = s.TotalPaidAmount,
            TotalRemainingAmount = s.TotalRemainingAmount,
            AdvanceBalance = s.AdvanceBalance,
            DebtCount = s.DebtCount
        }).ToList();
        return (PagedDataAppDto<VendorDebtSummaryAppDto>)PagedDataAppDto.Create(
            mappedItems,
            paged.PagerInfo.PageIndex,
            paged.PagerInfo.PageSize,
            paged.PagerInfo.TotalCount);
    }

    public async Task<VendorDebtsByVendorAppDto?> GetDebtsByVendorIdAsync(Guid vendorId)
    {
        var result = await _debtManager.GetDebtsByVendorIdAsync(vendorId).ConfigureAwait(false);
        if (result == null) return null;
        return new VendorDebtsByVendorAppDto
        {
            VendorId = result.VendorId,
            VendorName = result.VendorName,
            TotalDebtAmount = result.TotalDebtAmount,
            TotalPaidAmount = result.TotalPaidAmount,
            TotalRemainingAmount = result.TotalRemainingAmount,
            AdvanceBalance = result.AdvanceBalance,
            Debts = result.Debts.Select(d => d.ToDto()).ToList(),
            AdvancePayments = result.AdvancePayments.Select(p => p.ToDto()).ToList(),
            RecentPayments = result.RecentPayments.Select(p => p.ToDto()).ToList()
        };
    }

    public async Task<PagedDataAppDto<VendorDebtAppDto>> GetDebtsAsync(
        Guid? vendorId = null,
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15)
    {
        var paged = await _debtManager.GetDebtsAsync(vendorId, keywords, pageIndex, pageSize).ConfigureAwait(false);
        var mappedItems = paged.Items.Select(d => d.ToDto()).ToList();
        return (PagedDataAppDto<VendorDebtAppDto>)PagedDataAppDto.Create(
            mappedItems,
            paged.PagerInfo.PageIndex,
            paged.PagerInfo.PageSize,
            paged.PagerInfo.TotalCount);
    }

    public async Task<PagedDataAppDto<VendorPaymentAppDto>> GetPaymentsAsync(
        Guid? vendorId = null,
        int pageIndex = 0,
        int pageSize = 15)
    {
        var paged = await _debtManager.GetPaymentsAsync(vendorId, pageIndex, pageSize).ConfigureAwait(false);
        var mappedItems = paged.Items.Select(p => p.ToDto()).ToList();
        return (PagedDataAppDto<VendorPaymentAppDto>)PagedDataAppDto.Create(
            mappedItems,
            paged.PagerInfo.PageIndex,
            paged.PagerInfo.PageSize,
            paged.PagerInfo.TotalCount);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static CreateVendorDebtDto MapToDomainDto(CreateVendorDebtAppDto dto)
        => new()
        {
            VendorId = dto.VendorId,
            PurchaseOrderId = dto.PurchaseOrderId,
            TotalAmount = dto.TotalAmount,
            DueDateUtc = dto.DueDateUtc,
            CreatedByUserId = dto.CreatedByUserId
        };

    private static CreateVendorPaymentDto MapToDomainDto(CreateVendorPaymentAppDto dto)
        => new()
        {
            VendorId = dto.VendorId,
            VendorDebtId = dto.VendorDebtId,
            PurchaseOrderId = dto.PurchaseOrderId,
            Amount = dto.Amount,
            PaymentMethod = (PaymentMethod)dto.PaymentMethod,
            PaymentType = (PaymentType)dto.PaymentType,
            Note = dto.Note,
            PaidOnUtc = dto.PaidOnUtc,
            RecordedByUserId = dto.RecordedByUserId
        };
}

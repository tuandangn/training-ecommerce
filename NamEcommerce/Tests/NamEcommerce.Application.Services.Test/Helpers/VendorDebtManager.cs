using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Application.Services.Test.Helpers;

public static class VendorDebtManager
{
    // ── CreateDebtFromPurchaseOrderAsync ─────────────────────────────────────

    public static Mock<IVendorDebtManager> WhenCreateDebtReturns(CreateVendorDebtDto dto, VendorDebtDto @return)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.CreateDebtFromPurchaseOrderAsync(dto)).ReturnsAsync(@return).Verifiable();
        return mock;
    }

    public static Mock<IVendorDebtManager> WhenCreateDebtThrows(CreateVendorDebtDto dto, Exception ex)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.CreateDebtFromPurchaseOrderAsync(dto)).ThrowsAsync(ex).Verifiable();
        return mock;
    }

    // ── RecordPaymentAsync ───────────────────────────────────────────────────

    public static Mock<IVendorDebtManager> WhenRecordPaymentReturns(CreateVendorPaymentDto dto, VendorPaymentDto @return)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.RecordPaymentAsync(dto)).ReturnsAsync(@return).Verifiable();
        return mock;
    }

    public static Mock<IVendorDebtManager> WhenRecordPaymentThrows(CreateVendorPaymentDto dto, Exception ex)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.RecordPaymentAsync(dto)).ThrowsAsync(ex).Verifiable();
        return mock;
    }

    // ── RecordFlexiblePaymentForVendorAsync ──────────────────────────────────

    public static Mock<IVendorDebtManager> WhenFlexiblePaymentReturns(CreateVendorPaymentDto dto, IList<VendorPaymentDto> @return)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.RecordFlexiblePaymentForVendorAsync(dto)).ReturnsAsync(@return).Verifiable();
        return mock;
    }

    public static Mock<IVendorDebtManager> WhenFlexiblePaymentThrows(CreateVendorPaymentDto dto, Exception ex)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.RecordFlexiblePaymentForVendorAsync(dto)).ThrowsAsync(ex).Verifiable();
        return mock;
    }

    // ── RecordAdvancePaymentAsync ────────────────────────────────────────────

    public static Mock<IVendorDebtManager> WhenAdvancePaymentReturns(CreateVendorPaymentDto dto, VendorPaymentDto @return)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.RecordAdvancePaymentAsync(dto)).ReturnsAsync(@return).Verifiable();
        return mock;
    }

    // ── GetDebtByIdAsync ─────────────────────────────────────────────────────

    public static Mock<IVendorDebtManager> WhenGetDebtByIdReturns(Guid id, VendorDebtDto? @return)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.GetDebtByIdAsync(id)).ReturnsAsync(@return).Verifiable();
        return mock;
    }

    // ── GetPaymentByIdAsync ──────────────────────────────────────────────────

    public static Mock<IVendorDebtManager> WhenGetPaymentByIdReturns(Guid id, VendorPaymentDto? @return)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.GetPaymentByIdAsync(id)).ReturnsAsync(@return).Verifiable();
        return mock;
    }

    // ── GetVendorDebtSummaryAsync ────────────────────────────────────────────

    public static Mock<IVendorDebtManager> WhenGetSummaryReturns(Guid vendorId, VendorDebtSummaryDto? @return)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.GetVendorDebtSummaryAsync(vendorId)).ReturnsAsync(@return).Verifiable();
        return mock;
    }

    // ── GetVendorsWithDebtsAsync ─────────────────────────────────────────────

    public static Mock<IVendorDebtManager> WhenGetVendorsWithDebtsReturns(
        string? keywords,
        int pageIndex,
        int pageSize,
        IPagedDataDto<VendorDebtSummaryDto> @return)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.GetVendorsWithDebtsAsync(keywords, pageIndex, pageSize)).ReturnsAsync(@return).Verifiable();
        return mock;
    }

    // ── GetDebtsByVendorIdAsync ──────────────────────────────────────────────

    public static Mock<IVendorDebtManager> WhenGetDebtsByVendorIdReturns(Guid vendorId, VendorDebtsByVendorDto? @return)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.GetDebtsByVendorIdAsync(vendorId)).ReturnsAsync(@return).Verifiable();
        return mock;
    }

    // ── GetDebtsAsync ────────────────────────────────────────────────────────

    public static Mock<IVendorDebtManager> WhenGetDebtsReturns(
        Guid? vendorId,
        string? keywords,
        int pageIndex,
        int pageSize,
        IPagedDataDto<VendorDebtDto> @return)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.GetDebtsAsync(vendorId, keywords, pageIndex, pageSize)).ReturnsAsync(@return).Verifiable();
        return mock;
    }

    // ── GetPaymentsAsync ─────────────────────────────────────────────────────

    public static Mock<IVendorDebtManager> WhenGetPaymentsReturns(
        Guid? vendorId,
        int pageIndex,
        int pageSize,
        IPagedDataDto<VendorPaymentDto> @return)
    {
        var mock = new Mock<IVendorDebtManager>();
        mock.Setup(r => r.GetPaymentsAsync(vendorId, pageIndex, pageSize)).ReturnsAsync(@return).Verifiable();
        return mock;
    }
}

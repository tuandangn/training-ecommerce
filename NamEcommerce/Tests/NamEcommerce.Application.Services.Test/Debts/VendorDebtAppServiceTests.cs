using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Services.Debts;
using NamEcommerce.Application.Services.Test.Helpers;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Application.Services.Test.Debts;

public sealed class VendorDebtAppServiceTests
{
    // ── Shared test data factories ───────────────────────────────────────────

    private static VendorDebtDto MakeDebtDto(
        Guid? id = null,
        Guid? vendorId = null,
        Guid? purchaseOrderId = null,
        decimal totalAmount = 1_000_000m)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Code = "CNNCC-20260421-001",
            VendorId = vendorId ?? Guid.NewGuid(),
            VendorName = "NCC Test",
            PurchaseOrderId = purchaseOrderId ?? Guid.NewGuid(),
            PurchaseOrderCode = "PO-001",
            TotalAmount = totalAmount,
            PaidAmount = 0,
            RemainingAmount = totalAmount,
            Status = DebtStatus.Outstanding,
            CreatedOnUtc = DateTime.UtcNow
        };

    private static VendorPaymentDto MakePaymentDto(
        Guid? id = null,
        Guid? vendorId = null,
        decimal amount = 500_000m)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Code = "PCNCC-20260421-001",
            VendorId = vendorId ?? Guid.NewGuid(),
            VendorName = "NCC Test",
            Amount = amount,
            PaymentMethod = PaymentMethod.Cash,
            PaymentType = PaymentType.VendorDebtPayment,
            PaidOnUtc = DateTime.UtcNow,
            CreatedOnUtc = DateTime.UtcNow
        };

    private static VendorDebtSummaryDto MakeSummaryDto(Guid vendorId)
        => new()
        {
            VendorId = vendorId,
            VendorName = "NCC Test",
            TotalDebtAmount = 2_000_000m,
            TotalPaidAmount = 500_000m,
            TotalRemainingAmount = 1_500_000m,
            AdvanceBalance = 0m,
            DebtCount = 2
        };

    // ── CreateDebtFromPurchaseOrderAsync ─────────────────────────────────────

    #region CreateDebtFromPurchaseOrderAsync

    [Fact]
    public async Task CreateDebtFromPurchaseOrderAsync_DtoIsInvalid_ReturnsFalseResult()
    {
        var invalidDto = new CreateVendorDebtAppDto
        {
            VendorId = Guid.Empty,      // invalid
            PurchaseOrderId = Guid.NewGuid(),
            TotalAmount = 1_000_000m
        };
        var service = new VendorDebtAppService(null!);

        var result = await service.CreateDebtFromPurchaseOrderAsync(invalidDto);

        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage!);
    }

    [Fact]
    public async Task CreateDebtFromPurchaseOrderAsync_TotalAmountIsZero_ReturnsFalseResult()
    {
        var invalidDto = new CreateVendorDebtAppDto
        {
            VendorId = Guid.NewGuid(),
            PurchaseOrderId = Guid.NewGuid(),
            TotalAmount = 0m           // invalid
        };
        var service = new VendorDebtAppService(null!);

        var result = await service.CreateDebtFromPurchaseOrderAsync(invalidDto);

        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage!);
    }

    [Fact]
    public async Task CreateDebtFromPurchaseOrderAsync_ManagerThrows_ReturnsFalseResult()
    {
        var vendorId = Guid.NewGuid();
        var purchaseOrderId = Guid.NewGuid();
        var dto = new CreateVendorDebtAppDto
        {
            VendorId = vendorId,
            PurchaseOrderId = purchaseOrderId,
            TotalAmount = 1_000_000m
        };
        var domainDto = new CreateVendorDebtDto
        {
            VendorId = vendorId,
            PurchaseOrderId = purchaseOrderId,
            TotalAmount = 1_000_000m
        };
        var managerMock = VendorDebtManager.WhenCreateDebtThrows(domainDto, new Exception("Vendor not found"));
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.CreateDebtFromPurchaseOrderAsync(dto);

        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage!);
        managerMock.Verify();
    }

    [Fact]
    public async Task CreateDebtFromPurchaseOrderAsync_ValidDto_ReturnsSuccessResult()
    {
        var vendorId = Guid.NewGuid();
        var purchaseOrderId = Guid.NewGuid();
        var debtDto = MakeDebtDto(vendorId: vendorId, purchaseOrderId: purchaseOrderId);
        var dto = new CreateVendorDebtAppDto
        {
            VendorId = vendorId,
            PurchaseOrderId = purchaseOrderId,
            TotalAmount = debtDto.TotalAmount
        };
        var domainDto = new CreateVendorDebtDto
        {
            VendorId = vendorId,
            PurchaseOrderId = purchaseOrderId,
            TotalAmount = debtDto.TotalAmount
        };
        var managerMock = VendorDebtManager.WhenCreateDebtReturns(domainDto, debtDto);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.CreateDebtFromPurchaseOrderAsync(dto);

        Assert.True(result.Success);
        Assert.Equal(debtDto.Id, result.CreatedId);
        Assert.NotNull(result.Debt);
        Assert.Equal(debtDto.Id, result.Debt.Id);
        Assert.Equal((int)debtDto.Status, result.Debt.Status);
        managerMock.Verify();
    }

    #endregion

    // ── RecordPaymentAsync ───────────────────────────────────────────────────

    #region RecordPaymentAsync

    [Fact]
    public async Task RecordPaymentAsync_DtoIsInvalid_ReturnsFalseResult()
    {
        var invalidDto = new CreateVendorPaymentAppDto
        {
            VendorId = Guid.Empty,      // invalid
            Amount = 500_000m,
            PaidOnUtc = DateTime.UtcNow
        };
        var service = new VendorDebtAppService(null!);

        var result = await service.RecordPaymentAsync(invalidDto);

        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage!);
    }

    [Fact]
    public async Task RecordPaymentAsync_AmountIsZero_ReturnsFalseResult()
    {
        var invalidDto = new CreateVendorPaymentAppDto
        {
            VendorId = Guid.NewGuid(),
            Amount = 0m,                // invalid
            PaidOnUtc = DateTime.UtcNow
        };
        var service = new VendorDebtAppService(null!);

        var result = await service.RecordPaymentAsync(invalidDto);

        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage!);
    }

    [Fact]
    public async Task RecordPaymentAsync_ManagerThrows_ReturnsFalseResult()
    {
        var vendorId = Guid.NewGuid();
        var paidOn = DateTime.UtcNow;
        // PaymentMethod = 0 → Cash; PaymentType = 50 → VendorDebtPayment (explicit to match MapToDomainDto cast)
        var dto = new CreateVendorPaymentAppDto
        {
            VendorId = vendorId,
            Amount = 500_000m,
            PaymentMethod = (int)PaymentMethod.Cash,
            PaymentType = (int)PaymentType.VendorDebtPayment,
            PaidOnUtc = paidOn
        };
        var domainDto = new CreateVendorPaymentDto
        {
            VendorId = vendorId,
            Amount = 500_000m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentType = PaymentType.VendorDebtPayment,
            PaidOnUtc = paidOn
        };
        var managerMock = VendorDebtManager.WhenRecordPaymentThrows(domainDto, new Exception("Exceeds remaining"));
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.RecordPaymentAsync(dto);

        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage!);
        managerMock.Verify();
    }

    [Fact]
    public async Task RecordPaymentAsync_ValidDto_ReturnsSuccessResult()
    {
        var vendorId = Guid.NewGuid();
        var paidOn = DateTime.UtcNow;
        var paymentDto = MakePaymentDto(vendorId: vendorId);
        var dto = new CreateVendorPaymentAppDto
        {
            VendorId = vendorId,
            Amount = paymentDto.Amount,
            PaymentMethod = (int)PaymentMethod.Cash,
            PaymentType = (int)PaymentType.VendorDebtPayment,
            PaidOnUtc = paidOn
        };
        var domainDto = new CreateVendorPaymentDto
        {
            VendorId = vendorId,
            Amount = paymentDto.Amount,
            PaymentMethod = PaymentMethod.Cash,
            PaymentType = PaymentType.VendorDebtPayment,
            PaidOnUtc = paidOn
        };
        var managerMock = VendorDebtManager.WhenRecordPaymentReturns(domainDto, paymentDto);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.RecordPaymentAsync(dto);

        Assert.True(result.Success);
        Assert.Equal(paymentDto.Id, result.CreatedId);
        Assert.NotNull(result.Payment);
        Assert.Equal(paymentDto.Amount, result.Payment.Amount);
        managerMock.Verify();
    }

    #endregion

    // ── RecordFlexiblePaymentForVendorAsync ──────────────────────────────────

    #region RecordFlexiblePaymentForVendorAsync

    [Fact]
    public async Task RecordFlexiblePaymentForVendorAsync_DtoIsInvalid_ReturnsFalseResult()
    {
        var invalidDto = new CreateVendorPaymentAppDto
        {
            VendorId = Guid.NewGuid(),
            Amount = -1m,               // invalid
            PaidOnUtc = DateTime.UtcNow
        };
        var service = new VendorDebtAppService(null!);

        var result = await service.RecordFlexiblePaymentForVendorAsync(invalidDto);

        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage!);
    }

    [Fact]
    public async Task RecordFlexiblePaymentForVendorAsync_ValidDto_ReturnsPaymentsList()
    {
        var vendorId = Guid.NewGuid();
        var paidOn = DateTime.UtcNow;
        var payment1 = MakePaymentDto(vendorId: vendorId, amount: 300_000m);
        var payment2 = MakePaymentDto(vendorId: vendorId, amount: 200_000m);
        var dto = new CreateVendorPaymentAppDto
        {
            VendorId = vendorId,
            Amount = 500_000m,
            PaymentMethod = (int)PaymentMethod.Cash,
            PaymentType = (int)PaymentType.VendorDebtPayment,
            PaidOnUtc = paidOn
        };
        var domainDto = new CreateVendorPaymentDto
        {
            VendorId = vendorId,
            Amount = 500_000m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentType = PaymentType.VendorDebtPayment,
            PaidOnUtc = paidOn
        };
        var managerMock = VendorDebtManager.WhenFlexiblePaymentReturns(domainDto, [payment1, payment2]);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.RecordFlexiblePaymentForVendorAsync(dto);

        Assert.True(result.Success);
        Assert.Equal(2, result.Payments.Count);
        Assert.Contains(result.Payments, p => p.Id == payment1.Id);
        Assert.Contains(result.Payments, p => p.Id == payment2.Id);
        managerMock.Verify();
    }

    [Fact]
    public async Task RecordFlexiblePaymentForVendorAsync_ManagerThrows_ReturnsFalseResult()
    {
        var vendorId = Guid.NewGuid();
        var paidOn = DateTime.UtcNow;
        var dto = new CreateVendorPaymentAppDto
        {
            VendorId = vendorId,
            Amount = 500_000m,
            PaymentMethod = (int)PaymentMethod.Cash,
            PaymentType = (int)PaymentType.VendorDebtPayment,
            PaidOnUtc = paidOn
        };
        var domainDto = new CreateVendorPaymentDto
        {
            VendorId = vendorId,
            Amount = 500_000m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentType = PaymentType.VendorDebtPayment,
            PaidOnUtc = paidOn
        };
        var managerMock = VendorDebtManager.WhenFlexiblePaymentThrows(domainDto, new Exception("Vendor not found"));
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.RecordFlexiblePaymentForVendorAsync(dto);

        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage!);
        managerMock.Verify();
    }

    #endregion

    // ── RecordAdvancePaymentAsync ────────────────────────────────────────────

    #region RecordAdvancePaymentAsync

    [Fact]
    public async Task RecordAdvancePaymentAsync_DtoIsInvalid_ReturnsFalseResult()
    {
        var invalidDto = new CreateVendorPaymentAppDto
        {
            VendorId = Guid.NewGuid(),
            Amount = 500_000m,
            PaidOnUtc = default     // invalid
        };
        var service = new VendorDebtAppService(null!);

        var result = await service.RecordAdvancePaymentAsync(invalidDto);

        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage!);
    }

    [Fact]
    public async Task RecordAdvancePaymentAsync_ValidDto_ReturnsSuccessResult()
    {
        var vendorId = Guid.NewGuid();
        var paidOn = DateTime.UtcNow;
        var paymentDto = MakePaymentDto(vendorId: vendorId, amount: 1_000_000m);
        var dto = new CreateVendorPaymentAppDto
        {
            VendorId = vendorId,
            Amount = paymentDto.Amount,
            PaymentMethod = (int)PaymentMethod.Cash,
            PaymentType = (int)PaymentType.AdvancePayment,
            PaidOnUtc = paidOn
        };
        var domainDto = new CreateVendorPaymentDto
        {
            VendorId = vendorId,
            Amount = paymentDto.Amount,
            PaymentMethod = PaymentMethod.Cash,
            PaymentType = PaymentType.AdvancePayment,
            PaidOnUtc = paidOn
        };
        var managerMock = VendorDebtManager.WhenAdvancePaymentReturns(domainDto, paymentDto);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.RecordAdvancePaymentAsync(dto);

        Assert.True(result.Success);
        Assert.Equal(paymentDto.Id, result.CreatedId);
        Assert.NotNull(result.Payment);
        managerMock.Verify();
    }

    #endregion

    // ── GetDebtByIdAsync ─────────────────────────────────────────────────────

    #region GetDebtByIdAsync

    [Fact]
    public async Task GetDebtByIdAsync_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var managerMock = VendorDebtManager.WhenGetDebtByIdReturns(id, null);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.GetDebtByIdAsync(id);

        Assert.Null(result);
        managerMock.Verify();
    }

    [Fact]
    public async Task GetDebtByIdAsync_Found_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var debtDto = MakeDebtDto(id: id);
        var managerMock = VendorDebtManager.WhenGetDebtByIdReturns(id, debtDto);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.GetDebtByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal((int)debtDto.Status, result.Status);
        managerMock.Verify();
    }

    #endregion

    // ── GetPaymentByIdAsync ──────────────────────────────────────────────────

    #region GetPaymentByIdAsync

    [Fact]
    public async Task GetPaymentByIdAsync_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var managerMock = VendorDebtManager.WhenGetPaymentByIdReturns(id, null);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.GetPaymentByIdAsync(id);

        Assert.Null(result);
        managerMock.Verify();
    }

    [Fact]
    public async Task GetPaymentByIdAsync_Found_ReturnsDto()
    {
        var id = Guid.NewGuid();
        var paymentDto = MakePaymentDto(id: id);
        var managerMock = VendorDebtManager.WhenGetPaymentByIdReturns(id, paymentDto);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.GetPaymentByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal((int)paymentDto.PaymentMethod, result.PaymentMethod);
        Assert.Equal((int)paymentDto.PaymentType, result.PaymentType);
        managerMock.Verify();
    }

    #endregion

    // ── GetVendorDebtSummaryAsync ────────────────────────────────────────────

    #region GetVendorDebtSummaryAsync

    [Fact]
    public async Task GetVendorDebtSummaryAsync_VendorNotFound_ReturnsNull()
    {
        var vendorId = Guid.NewGuid();
        var managerMock = VendorDebtManager.WhenGetSummaryReturns(vendorId, null);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.GetVendorDebtSummaryAsync(vendorId);

        Assert.Null(result);
        managerMock.Verify();
    }

    [Fact]
    public async Task GetVendorDebtSummaryAsync_Found_ReturnsSummary()
    {
        var vendorId = Guid.NewGuid();
        var summary = MakeSummaryDto(vendorId);
        var managerMock = VendorDebtManager.WhenGetSummaryReturns(vendorId, summary);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.GetVendorDebtSummaryAsync(vendorId);

        Assert.NotNull(result);
        Assert.Equal(vendorId, result.VendorId);
        Assert.Equal(summary.TotalRemainingAmount, result.TotalRemainingAmount);
        Assert.Equal(summary.DebtCount, result.DebtCount);
        managerMock.Verify();
    }

    #endregion

    // ── GetVendorsWithDebtsAsync ─────────────────────────────────────────────

    #region GetVendorsWithDebtsAsync

    [Fact]
    public async Task GetVendorsWithDebtsAsync_ReturnsPagedSummaries()
    {
        var pageIndex = 0;
        var pageSize = 10;
        var vendorId1 = Guid.NewGuid();
        var vendorId2 = Guid.NewGuid();
        var pagedData = PagedDataDto.Create(
            [MakeSummaryDto(vendorId1), MakeSummaryDto(vendorId2)],
            pageIndex, pageSize);
        var managerMock = VendorDebtManager.WhenGetVendorsWithDebtsReturns(null, pageIndex, pageSize, pagedData);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.GetVendorsWithDebtsAsync(null, pageIndex, pageSize);

        Assert.Equal(2, result.Pagination.TotalCount);
        Assert.Equal(vendorId1, result.Items.First().VendorId);
        managerMock.Verify();
    }

    #endregion

    // ── GetDebtsByVendorIdAsync ──────────────────────────────────────────────

    #region GetDebtsByVendorIdAsync

    [Fact]
    public async Task GetDebtsByVendorIdAsync_VendorNotFound_ReturnsNull()
    {
        var vendorId = Guid.NewGuid();
        var managerMock = VendorDebtManager.WhenGetDebtsByVendorIdReturns(vendorId, null);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.GetDebtsByVendorIdAsync(vendorId);

        Assert.Null(result);
        managerMock.Verify();
    }

    [Fact]
    public async Task GetDebtsByVendorIdAsync_Found_ReturnsVendorDebts()
    {
        var vendorId = Guid.NewGuid();
        var debt = MakeDebtDto(vendorId: vendorId);
        var payment = MakePaymentDto(vendorId: vendorId);
        var domainResult = new VendorDebtsByVendorDto
        {
            VendorId = vendorId,
            VendorName = "NCC Test",
            TotalDebtAmount = debt.TotalAmount,
            TotalPaidAmount = 0m,
            TotalRemainingAmount = debt.TotalAmount,
            AdvanceBalance = payment.Amount,
            Debts = [debt],
            AdvancePayments = [payment],
            RecentPayments = [payment]
        };
        var managerMock = VendorDebtManager.WhenGetDebtsByVendorIdReturns(vendorId, domainResult);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.GetDebtsByVendorIdAsync(vendorId);

        Assert.NotNull(result);
        Assert.Equal(vendorId, result.VendorId);
        Assert.Single(result.Debts);
        Assert.Single(result.AdvancePayments);
        Assert.Single(result.RecentPayments);
        managerMock.Verify();
    }

    #endregion

    // ── GetDebtsAsync ────────────────────────────────────────────────────────

    #region GetDebtsAsync

    [Fact]
    public async Task GetDebtsAsync_ReturnsPagedDebts()
    {
        var vendorId = Guid.NewGuid();
        var pageIndex = 0;
        var pageSize = 15;
        var debt = MakeDebtDto(vendorId: vendorId);
        var pagedData = PagedDataDto.Create([debt], pageIndex, pageSize);
        var managerMock = VendorDebtManager.WhenGetDebtsReturns(vendorId, null, pageIndex, pageSize, pagedData);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.GetDebtsAsync(vendorId, null, pageIndex, pageSize);

        Assert.Equal(1, result.Pagination.TotalCount);
        Assert.Equal(debt.Id, result.Items.First().Id);
        managerMock.Verify();
    }

    #endregion

    // ── GetPaymentsAsync ─────────────────────────────────────────────────────

    #region GetPaymentsAsync

    [Fact]
    public async Task GetPaymentsAsync_ReturnsPagedPayments()
    {
        var vendorId = Guid.NewGuid();
        var pageIndex = 0;
        var pageSize = 15;
        var payment = MakePaymentDto(vendorId: vendorId);
        var pagedData = PagedDataDto.Create([payment], pageIndex, pageSize);
        var managerMock = VendorDebtManager.WhenGetPaymentsReturns(vendorId, pageIndex, pageSize, pagedData);
        var service = new VendorDebtAppService(managerMock.Object);

        var result = await service.GetPaymentsAsync(vendorId, pageIndex, pageSize);

        Assert.Equal(1, result.Pagination.TotalCount);
        Assert.Equal(payment.Id, result.Items.First().Id);
        Assert.Equal((int)payment.PaymentMethod, result.Items.First().PaymentMethod);
        managerMock.Verify();
    }

    #endregion
}

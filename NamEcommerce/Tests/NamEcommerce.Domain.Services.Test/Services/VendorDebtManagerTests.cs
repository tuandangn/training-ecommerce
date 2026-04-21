using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Services.Debts;
using NamEcommerce.Domain.Services.Test.Helpers;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Debts;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class VendorDebtManagerTests
{
    // Helper: tạo VendorDebtManager với các tham số đơn giản nhất
    private static VendorDebtManager CreateManager(
        Mock<IRepository<VendorDebt>>? debtRepo = null,
        Mock<IEntityDataReader<VendorDebt>>? debtReader = null,
        Mock<IRepository<VendorPayment>>? paymentRepo = null,
        Mock<IEntityDataReader<VendorPayment>>? paymentReader = null,
        Mock<IEntityDataReader<Vendor>>? vendorReader = null,
        Mock<IEntityDataReader<PurchaseOrder>>? purchaseOrderReader = null,
        IEventPublisher? eventPublisher = null)
        => new VendorDebtManager(
            debtRepo?.Object ?? null!,
            debtReader?.Object ?? null!,
            paymentRepo?.Object ?? null!,
            paymentReader?.Object ?? null!,
            vendorReader?.Object ?? null!,
            purchaseOrderReader?.Object ?? null!,
            eventPublisher ?? Mock.Of<IEventPublisher>());

    #region CreateDebtFromPurchaseOrderAsync

    [Fact]
    public async Task CreateDebtFromPurchaseOrderAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = CreateManager();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => manager.CreateDebtFromPurchaseOrderAsync(null!));
    }

    [Fact]
    public async Task CreateDebtFromPurchaseOrderAsync_DtoIsInvalid_ThrowsArgumentException()
    {
        var invalidDto = new CreateVendorDebtDto
        {
            VendorId = Guid.Empty,
            PurchaseOrderId = Guid.NewGuid(),
            TotalAmount = 0
        };
        var manager = CreateManager();

        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.CreateDebtFromPurchaseOrderAsync(invalidDto));
    }

    [Fact]
    public async Task CreateDebtFromPurchaseOrderAsync_DebtExistsForPO_ReturnsExisting()
    {
        var purchaseOrderId = Guid.NewGuid();
        var existingDebt = new VendorDebt(
            code: "CNNCC-20260101-001",
            vendorId: Guid.NewGuid(),
            vendorName: "NCC A",
            purchaseOrderId: purchaseOrderId,
            purchaseOrderCode: "PO-001",
            totalAmount: 1_000_000,
            dueDateUtc: null,
            createdByUserId: null);

        var debtReaderStub = VendorDebtDataReader.WithData(existingDebt);
        var paymentReaderStub = VendorPaymentDataReader.Empty();
        var manager = CreateManager(
            debtReader: debtReaderStub,
            paymentReader: paymentReaderStub);

        var dto = new CreateVendorDebtDto
        {
            VendorId = existingDebt.VendorId,
            PurchaseOrderId = purchaseOrderId,
            TotalAmount = 1_000_000
        };

        var result = await manager.CreateDebtFromPurchaseOrderAsync(dto);

        Assert.Equal(existingDebt.Id, result.Id);
        Assert.Equal(existingDebt.Code, result.Code);
    }

    [Fact]
    public async Task CreateDebtFromPurchaseOrderAsync_VendorNotFound_ThrowsArgumentException()
    {
        var notFoundVendorId = Guid.NewGuid();
        var dto = new CreateVendorDebtDto
        {
            VendorId = notFoundVendorId,
            PurchaseOrderId = Guid.NewGuid(),
            TotalAmount = 500_000
        };
        var debtReaderStub = VendorDebtDataReader.Empty();
        var vendorReaderMock = VendorDataReader.NotFound(notFoundVendorId);
        var paymentReaderStub = VendorPaymentDataReader.Empty();
        var manager = CreateManager(
            debtReader: debtReaderStub,
            paymentReader: paymentReaderStub,
            vendorReader: vendorReaderMock);

        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.CreateDebtFromPurchaseOrderAsync(dto));

        vendorReaderMock.Verify();
    }

    [Fact]
    public async Task CreateDebtFromPurchaseOrderAsync_PurchaseOrderNotFound_ThrowsArgumentException()
    {
        var vendor = new Vendor(Guid.NewGuid(), "NCC A", "0901234567");
        var notFoundPoId = Guid.NewGuid();
        var dto = new CreateVendorDebtDto
        {
            VendorId = vendor.Id,
            PurchaseOrderId = notFoundPoId,
            TotalAmount = 500_000
        };
        var debtReaderStub = VendorDebtDataReader.Empty();
        var vendorReaderStub = VendorDataReader.VendorById(vendor.Id, vendor);
        var purchaseOrderReaderMock = PurchaseOrderDataReader.NotFound(notFoundPoId);
        var paymentReaderStub = VendorPaymentDataReader.Empty();
        var manager = CreateManager(
            debtReader: debtReaderStub,
            paymentReader: paymentReaderStub,
            vendorReader: vendorReaderStub,
            purchaseOrderReader: purchaseOrderReaderMock);

        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.CreateDebtFromPurchaseOrderAsync(dto));

        purchaseOrderReaderMock.Verify();
    }

    [Fact]
    public async Task CreateDebtFromPurchaseOrderAsync_ValidInput_CreatesDebt()
    {
        var vendor = new Vendor(Guid.NewGuid(), "NCC Minh Hòa", "0901234567");
        var purchaseOrder = new PurchaseOrder("PO-20260101-001", vendor.Id, null, null);
        var dto = new CreateVendorDebtDto
        {
            VendorId = vendor.Id,
            PurchaseOrderId = purchaseOrder.Id,
            TotalAmount = 2_000_000
        };

        var expectedDebt = new VendorDebt(
            code: "CNNCC-20260101-001",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            purchaseOrderId: purchaseOrder.Id,
            purchaseOrderCode: purchaseOrder.Code,
            totalAmount: dto.TotalAmount,
            dueDateUtc: null,
            createdByUserId: null);

        var debtReaderStub = VendorDebtDataReader.Empty();
        var vendorReaderStub = VendorDataReader.VendorById(vendor.Id, vendor);
        var purchaseOrderReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var paymentReaderStub = VendorPaymentDataReader.Empty();
        var debtRepoMock = VendorDebtRepository.InsertWillReturn(expectedDebt);
        var paymentRepoStub = new Mock<IRepository<VendorPayment>>();
        paymentRepoStub.Setup(r => r.UpdateAsync(It.IsAny<VendorPayment>(), default)).ReturnsAsync((VendorPayment p, CancellationToken _) => p);

        var manager = CreateManager(
            debtRepo: debtRepoMock,
            debtReader: debtReaderStub,
            paymentRepo: paymentRepoStub,
            paymentReader: paymentReaderStub,
            vendorReader: vendorReaderStub,
            purchaseOrderReader: purchaseOrderReaderStub);

        var result = await manager.CreateDebtFromPurchaseOrderAsync(dto);

        Assert.Equal(expectedDebt.Id, result.Id);
        Assert.Equal(dto.TotalAmount, result.TotalAmount);
        Assert.Equal(DebtStatus.Outstanding, result.Status);
        debtRepoMock.Verify();
    }

    [Fact]
    public async Task CreateDebtFromPurchaseOrderAsync_WithAdvancePayments_AutoApplies()
    {
        var vendor = new Vendor(Guid.NewGuid(), "NCC B", "0902222222");
        var purchaseOrder = new PurchaseOrder("PO-20260101-002", vendor.Id, null, null);
        var totalAmount = 1_000_000m;
        var advanceAmount = 300_000m;

        // Tạo advance payment chưa áp dụng
        var advancePayment = new VendorPayment(
            code: "PCNCC-20260101-001",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            amount: advanceAmount,
            paymentMethod: PaymentMethod.Cash,
            paymentType: PaymentType.AdvancePayment,
            paidOnUtc: DateTime.UtcNow.AddDays(-1),
            recordedByUserId: null,
            note: null);

        var dto = new CreateVendorDebtDto
        {
            VendorId = vendor.Id,
            PurchaseOrderId = purchaseOrder.Id,
            TotalAmount = totalAmount
        };

        var expectedDebt = new VendorDebt(
            code: "CNNCC-20260101-001",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            purchaseOrderId: purchaseOrder.Id,
            purchaseOrderCode: purchaseOrder.Code,
            totalAmount: totalAmount,
            dueDateUtc: null,
            createdByUserId: null);

        var debtReaderStub = VendorDebtDataReader.Empty();
        var vendorReaderStub = VendorDataReader.VendorById(vendor.Id, vendor);
        var purchaseOrderReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var paymentReaderStub = VendorPaymentDataReader.WithData(advancePayment);

        // Capture thực thể thực sự sau khi đã ApplyPayment
        var debtRepoMock = new Mock<IRepository<VendorDebt>>();
        debtRepoMock.Setup(r => r.InsertAsync(It.IsAny<VendorDebt>(), default))
            .ReturnsAsync((VendorDebt d, CancellationToken _) => d);

        var paymentRepoMock = new Mock<IRepository<VendorPayment>>();
        paymentRepoMock.Setup(r => r.UpdateAsync(It.IsAny<VendorPayment>(), default))
            .ReturnsAsync((VendorPayment p, CancellationToken _) => p)
            .Verifiable();

        var manager = CreateManager(
            debtRepo: debtRepoMock,
            debtReader: debtReaderStub,
            paymentRepo: paymentRepoMock,
            paymentReader: paymentReaderStub,
            vendorReader: vendorReaderStub,
            purchaseOrderReader: purchaseOrderReaderStub);

        var result = await manager.CreateDebtFromPurchaseOrderAsync(dto);

        // Advance payment phải được áp dụng → UpdateAsync được gọi
        paymentRepoMock.Verify();
        // Sau khi auto-apply, PaidAmount = advanceAmount
        Assert.Equal(advanceAmount, result.PaidAmount);
        Assert.Equal(totalAmount - advanceAmount, result.RemainingAmount);
        Assert.Equal(DebtStatus.PartiallyPaid, result.Status);
    }

    #endregion

    #region RecordPaymentAsync

    [Fact]
    public async Task RecordPaymentAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = CreateManager();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => manager.RecordPaymentAsync(null!));
    }

    [Fact]
    public async Task RecordPaymentAsync_DtoIsInvalid_ThrowsArgumentException()
    {
        var invalidDto = new CreateVendorPaymentDto
        {
            VendorId = Guid.NewGuid(),
            Amount = 0,
            PaidOnUtc = DateTime.UtcNow
        };
        var manager = CreateManager();

        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.RecordPaymentAsync(invalidDto));
    }

    [Fact]
    public async Task RecordPaymentAsync_VendorNotFound_ThrowsArgumentException()
    {
        var notFoundVendorId = Guid.NewGuid();
        var dto = new CreateVendorPaymentDto
        {
            VendorId = notFoundVendorId,
            Amount = 500_000,
            PaymentMethod = PaymentMethod.Cash,
            PaymentType = PaymentType.VendorDebtPayment,
            PaidOnUtc = DateTime.UtcNow
        };
        var vendorReaderMock = VendorDataReader.NotFound(notFoundVendorId);
        var debtReaderStub = VendorDebtDataReader.Empty();
        var paymentReaderStub = VendorPaymentDataReader.Empty();
        var manager = CreateManager(
            debtReader: debtReaderStub,
            paymentReader: paymentReaderStub,
            vendorReader: vendorReaderMock);

        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.RecordPaymentAsync(dto));

        vendorReaderMock.Verify();
    }

    [Fact]
    public async Task RecordPaymentAsync_ValidDebt_ReducesRemainingAmount()
    {
        var vendor = new Vendor(Guid.NewGuid(), "NCC C", "0903333333");
        var debt = new VendorDebt(
            code: "CNNCC-20260101-001",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderCode: "PO-001",
            totalAmount: 1_000_000,
            dueDateUtc: null,
            createdByUserId: null);
        var paymentAmount = 400_000m;
        var dto = new CreateVendorPaymentDto
        {
            VendorId = vendor.Id,
            VendorDebtId = debt.Id,
            Amount = paymentAmount,
            PaymentMethod = PaymentMethod.Cash,
            PaymentType = PaymentType.VendorDebtPayment,
            PaidOnUtc = DateTime.UtcNow
        };
        var expectedPayment = new VendorPayment(
            code: "PCNCC-20260101-001",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            amount: paymentAmount,
            paymentMethod: PaymentMethod.Cash,
            paymentType: PaymentType.VendorDebtPayment,
            paidOnUtc: dto.PaidOnUtc,
            recordedByUserId: null,
            note: null)
        { VendorDebtId = debt.Id };

        var vendorReaderStub = VendorDataReader.VendorById(vendor.Id, vendor);
        var debtReaderStub = VendorDebtDataReader.Empty();
        var paymentReaderStub = VendorPaymentDataReader.Empty();
        var debtRepoMock = new Mock<IRepository<VendorDebt>>();
        debtRepoMock.Setup(r => r.GetByIdAsync(debt.Id, default)).ReturnsAsync(debt).Verifiable();
        debtRepoMock.Setup(r => r.UpdateAsync(It.Is<VendorDebt>(d => d.Id == debt.Id), default))
            .ReturnsAsync(debt).Verifiable();
        var paymentRepoMock = new Mock<IRepository<VendorPayment>>();
        paymentRepoMock.Setup(r => r.InsertAsync(It.IsAny<VendorPayment>(), default))
            .ReturnsAsync((VendorPayment p, CancellationToken _) => p);

        var manager = CreateManager(
            debtRepo: debtRepoMock,
            debtReader: debtReaderStub,
            paymentRepo: paymentRepoMock,
            paymentReader: paymentReaderStub,
            vendorReader: vendorReaderStub);

        var result = await manager.RecordPaymentAsync(dto);

        Assert.Equal(paymentAmount, result.Amount);
        Assert.Equal(debt.Id, result.VendorDebtId);
        // Debt RemainingAmount giảm
        Assert.Equal(1_000_000 - paymentAmount, debt.RemainingAmount);
        Assert.Equal(DebtStatus.PartiallyPaid, debt.Status);
        debtRepoMock.Verify();
    }

    [Fact]
    public async Task RecordPaymentAsync_ExceedsRemaining_ThrowsVendorPaymentExceedsRemainingException()
    {
        var vendor = new Vendor(Guid.NewGuid(), "NCC D", "0904444444");
        var debt = new VendorDebt(
            code: "CNNCC-20260101-001",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderCode: "PO-001",
            totalAmount: 500_000,
            dueDateUtc: null,
            createdByUserId: null);
        var exceedingAmount = 600_000m; // vượt quá TotalAmount
        var dto = new CreateVendorPaymentDto
        {
            VendorId = vendor.Id,
            VendorDebtId = debt.Id,
            Amount = exceedingAmount,
            PaymentMethod = PaymentMethod.Cash,
            PaymentType = PaymentType.VendorDebtPayment,
            PaidOnUtc = DateTime.UtcNow
        };

        var vendorReaderStub = VendorDataReader.VendorById(vendor.Id, vendor);
        var debtReaderStub = VendorDebtDataReader.Empty();
        var paymentReaderStub = VendorPaymentDataReader.Empty();
        var debtRepoMock = new Mock<IRepository<VendorDebt>>();
        debtRepoMock.Setup(r => r.GetByIdAsync(debt.Id, default)).ReturnsAsync(debt);
        var paymentRepoStub = new Mock<IRepository<VendorPayment>>();

        var manager = CreateManager(
            debtRepo: debtRepoMock,
            debtReader: debtReaderStub,
            paymentRepo: paymentRepoStub,
            paymentReader: paymentReaderStub,
            vendorReader: vendorReaderStub);

        await Assert.ThrowsAsync<VendorPaymentExceedsRemainingException>(
            () => manager.RecordPaymentAsync(dto));
    }

    [Fact]
    public async Task RecordPaymentAsync_PaysFull_UpdatesStatusToFullyPaid()
    {
        var vendor = new Vendor(Guid.NewGuid(), "NCC E", "0905555555");
        var totalAmount = 800_000m;
        var debt = new VendorDebt(
            code: "CNNCC-20260101-001",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderCode: "PO-001",
            totalAmount: totalAmount,
            dueDateUtc: null,
            createdByUserId: null);
        var dto = new CreateVendorPaymentDto
        {
            VendorId = vendor.Id,
            VendorDebtId = debt.Id,
            Amount = totalAmount, // trả đủ
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentType = PaymentType.VendorDebtPayment,
            PaidOnUtc = DateTime.UtcNow
        };

        var vendorReaderStub = VendorDataReader.VendorById(vendor.Id, vendor);
        var debtReaderStub = VendorDebtDataReader.Empty();
        var paymentReaderStub = VendorPaymentDataReader.Empty();
        var debtRepoMock = new Mock<IRepository<VendorDebt>>();
        debtRepoMock.Setup(r => r.GetByIdAsync(debt.Id, default)).ReturnsAsync(debt);
        debtRepoMock.Setup(r => r.UpdateAsync(It.IsAny<VendorDebt>(), default))
            .ReturnsAsync((VendorDebt d, CancellationToken _) => d);
        var paymentRepoStub = new Mock<IRepository<VendorPayment>>();
        paymentRepoStub.Setup(r => r.InsertAsync(It.IsAny<VendorPayment>(), default))
            .ReturnsAsync((VendorPayment p, CancellationToken _) => p);

        var manager = CreateManager(
            debtRepo: debtRepoMock,
            debtReader: debtReaderStub,
            paymentRepo: paymentRepoStub,
            paymentReader: paymentReaderStub,
            vendorReader: vendorReaderStub);

        await manager.RecordPaymentAsync(dto);

        Assert.Equal(DebtStatus.FullyPaid, debt.Status);
        Assert.Equal(0, debt.RemainingAmount);
    }

    #endregion

    #region RecordFlexiblePaymentForVendorAsync

    [Fact]
    public async Task RecordFlexiblePaymentForVendorAsync_MultipleDebts_AppliesFifo()
    {
        var vendor = new Vendor(Guid.NewGuid(), "NCC FIFO", "0906666666");
        var debt1 = new VendorDebt(
            code: "CNNCC-001",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderCode: "PO-001",
            totalAmount: 300_000,
            dueDateUtc: null,
            createdByUserId: null);
        var debt2 = new VendorDebt(
            code: "CNNCC-002",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderCode: "PO-002",
            totalAmount: 500_000,
            dueDateUtc: null,
            createdByUserId: null);

        var paymentAmount = 700_000m; // đủ trả hết debt1 (300k) + 400k của debt2
        var dto = new CreateVendorPaymentDto
        {
            VendorId = vendor.Id,
            Amount = paymentAmount,
            PaymentMethod = PaymentMethod.Cash,
            PaymentType = PaymentType.VendorDebtPayment,
            PaidOnUtc = DateTime.UtcNow
        };

        var debtReaderStub = VendorDebtDataReader.WithData(debt1, debt2);
        var vendorReaderStub = VendorDataReader.VendorById(vendor.Id, vendor);
        var paymentReaderStub = VendorPaymentDataReader.Empty();
        var debtRepoMock = new Mock<IRepository<VendorDebt>>();
        debtRepoMock.Setup(r => r.UpdateAsync(It.IsAny<VendorDebt>(), default))
            .ReturnsAsync((VendorDebt d, CancellationToken _) => d);
        var paymentRepoMock = new Mock<IRepository<VendorPayment>>();
        paymentRepoMock.Setup(r => r.InsertAsync(It.IsAny<VendorPayment>(), default))
            .ReturnsAsync((VendorPayment p, CancellationToken _) => p);

        var manager = CreateManager(
            debtRepo: debtRepoMock,
            debtReader: debtReaderStub,
            paymentRepo: paymentRepoMock,
            paymentReader: paymentReaderStub,
            vendorReader: vendorReaderStub);

        var results = await manager.RecordFlexiblePaymentForVendorAsync(dto);

        // 2 payments: 300k cho debt1 và 400k cho debt2
        Assert.Equal(2, results.Count);
        Assert.Equal(300_000, results[0].Amount);
        Assert.Equal(400_000, results[1].Amount);
        Assert.Equal(DebtStatus.FullyPaid, debt1.Status);
        Assert.Equal(100_000, debt2.RemainingAmount);
    }

    [Fact]
    public async Task RecordFlexiblePaymentForVendorAsync_OverpayAmount_CreatesAdvancePayment()
    {
        var vendor = new Vendor(Guid.NewGuid(), "NCC Overpay", "0907777777");
        var debt = new VendorDebt(
            code: "CNNCC-001",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderCode: "PO-001",
            totalAmount: 200_000,
            dueDateUtc: null,
            createdByUserId: null);

        var paymentAmount = 350_000m; // dư 150k sau khi trả hết debt
        var dto = new CreateVendorPaymentDto
        {
            VendorId = vendor.Id,
            Amount = paymentAmount,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentType = PaymentType.VendorDebtPayment,
            PaidOnUtc = DateTime.UtcNow
        };

        var debtReaderStub = VendorDebtDataReader.WithData(debt);
        var vendorReaderStub = VendorDataReader.VendorById(vendor.Id, vendor);
        var paymentReaderStub = VendorPaymentDataReader.Empty();
        var debtRepoMock = new Mock<IRepository<VendorDebt>>();
        debtRepoMock.Setup(r => r.UpdateAsync(It.IsAny<VendorDebt>(), default))
            .ReturnsAsync((VendorDebt d, CancellationToken _) => d);
        var paymentRepoMock = new Mock<IRepository<VendorPayment>>();
        paymentRepoMock.Setup(r => r.InsertAsync(It.IsAny<VendorPayment>(), default))
            .ReturnsAsync((VendorPayment p, CancellationToken _) => p);

        var manager = CreateManager(
            debtRepo: debtRepoMock,
            debtReader: debtReaderStub,
            paymentRepo: paymentRepoMock,
            paymentReader: paymentReaderStub,
            vendorReader: vendorReaderStub);

        var results = await manager.RecordFlexiblePaymentForVendorAsync(dto);

        // 2 payments: 200k trả debt + 150k advance
        Assert.Equal(2, results.Count);
        Assert.Equal(200_000, results[0].Amount);
        Assert.Equal(150_000, results[1].Amount);
        Assert.Equal(PaymentType.AdvancePayment, results[1].PaymentType);
    }

    #endregion

    #region RecordAdvancePaymentAsync

    [Fact]
    public async Task RecordAdvancePaymentAsync_CreatesUnappliedPayment()
    {
        var vendor = new Vendor(Guid.NewGuid(), "NCC Advance", "0908888888");
        var advanceAmount = 500_000m;
        var dto = new CreateVendorPaymentDto
        {
            VendorId = vendor.Id,
            Amount = advanceAmount,
            PaymentMethod = PaymentMethod.Cash,
            PaymentType = PaymentType.AdvancePayment,
            PaidOnUtc = DateTime.UtcNow
        };
        var expectedPayment = new VendorPayment(
            code: "PCNCC-20260101-001",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            amount: advanceAmount,
            paymentMethod: PaymentMethod.Cash,
            paymentType: PaymentType.AdvancePayment,
            paidOnUtc: dto.PaidOnUtc,
            recordedByUserId: null,
            note: null);

        var debtReaderStub = VendorDebtDataReader.Empty();
        var vendorReaderStub = VendorDataReader.VendorById(vendor.Id, vendor);
        var paymentReaderStub = VendorPaymentDataReader.Empty();
        var paymentRepoMock = new Mock<IRepository<VendorPayment>>();
        paymentRepoMock.Setup(r => r.InsertAsync(
            It.Is<VendorPayment>(p =>
                p.VendorId == vendor.Id
                && p.Amount == advanceAmount
                && p.PaymentType == PaymentType.AdvancePayment
                && !p.IsApplied), default))
            .ReturnsAsync(expectedPayment)
            .Verifiable();

        var manager = CreateManager(
            debtReader: debtReaderStub,
            paymentRepo: paymentRepoMock,
            paymentReader: paymentReaderStub,
            vendorReader: vendorReaderStub);

        var result = await manager.RecordAdvancePaymentAsync(dto);

        Assert.Equal(advanceAmount, result.Amount);
        Assert.Equal(PaymentType.AdvancePayment, result.PaymentType);
        Assert.Null(result.VendorDebtId);
        paymentRepoMock.Verify();
    }

    #endregion

    #region Status Transitions

    [Fact]
    public async Task VendorDebt_StatusTransition_OutstandingToPartiallyPaidToFullyPaid()
    {
        var vendor = new Vendor(Guid.NewGuid(), "NCC Status", "0909999999");
        var totalAmount = 1_000_000m;
        var debt = new VendorDebt(
            code: "CNNCC-001",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderCode: "PO-001",
            totalAmount: totalAmount,
            dueDateUtc: null,
            createdByUserId: null);

        // Bước 1: Outstanding
        Assert.Equal(DebtStatus.Outstanding, debt.Status);
        Assert.Equal(totalAmount, debt.RemainingAmount);

        // Bước 2: Thanh toán một phần → PartiallyPaid
        debt.ApplyPayment(400_000);
        Assert.Equal(DebtStatus.PartiallyPaid, debt.Status);
        Assert.Equal(600_000, debt.RemainingAmount);

        // Bước 3: Thanh toán hết → FullyPaid
        debt.ApplyPayment(600_000);
        Assert.Equal(DebtStatus.FullyPaid, debt.Status);
        Assert.Equal(0, debt.RemainingAmount);
    }

    #endregion

    #region GetDebtByIdAsync

    [Fact]
    public async Task GetDebtByIdAsync_NotFound_ReturnsNull()
    {
        var notFoundId = Guid.NewGuid();
        var debtReaderMock = VendorDebtDataReader.NotFound(notFoundId);
        var paymentReaderStub = VendorPaymentDataReader.Empty();
        var manager = CreateManager(
            debtReader: debtReaderMock,
            paymentReader: paymentReaderStub);

        var result = await manager.GetDebtByIdAsync(notFoundId);

        Assert.Null(result);
        debtReaderMock.Verify();
    }

    [Fact]
    public async Task GetDebtByIdAsync_Found_ReturnsDtoWithPayments()
    {
        var vendor = new Vendor(Guid.NewGuid(), "NCC Get", "0910000000");
        var debt = new VendorDebt(
            code: "CNNCC-001",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            purchaseOrderId: Guid.NewGuid(),
            purchaseOrderCode: "PO-001",
            totalAmount: 1_000_000,
            dueDateUtc: null,
            createdByUserId: null);

        var payment = new VendorPayment(
            code: "PCNCC-001",
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            amount: 200_000,
            paymentMethod: PaymentMethod.Cash,
            paymentType: PaymentType.VendorDebtPayment,
            paidOnUtc: DateTime.UtcNow,
            recordedByUserId: null,
            note: null)
        { VendorDebtId = debt.Id };

        var debtReaderMock = VendorDebtDataReader.DebtById(debt.Id, debt);
        var paymentReaderStub = VendorPaymentDataReader.WithData(payment);
        var manager = CreateManager(
            debtReader: debtReaderMock,
            paymentReader: paymentReaderStub);

        var result = await manager.GetDebtByIdAsync(debt.Id);

        Assert.NotNull(result);
        Assert.Equal(debt.Id, result.Id);
        Assert.Single(result.Payments);
        debtReaderMock.Verify();
    }

    #endregion

    #region GetPaymentByIdAsync

    [Fact]
    public async Task GetPaymentByIdAsync_NotFound_ReturnsNull()
    {
        var notFoundId = Guid.NewGuid();
        var debtReaderStub = VendorDebtDataReader.Empty();
        var paymentReaderMock = VendorPaymentDataReader.NotFound(notFoundId);
        var manager = CreateManager(
            debtReader: debtReaderStub,
            paymentReader: paymentReaderMock);

        var result = await manager.GetPaymentByIdAsync(notFoundId);

        Assert.Null(result);
        paymentReaderMock.Verify();
    }

    #endregion
}

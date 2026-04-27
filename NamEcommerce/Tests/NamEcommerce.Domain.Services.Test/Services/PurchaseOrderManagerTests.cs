using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Services.PurchaseOrders;
using NamEcommerce.Domain.Services.Test.Helpers;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Users;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class PurchaseOrderManagerTests
{
    #region Helper

    private Task<PurchaseOrder> CreatePurchaseOrder(string code, Guid vendorId, Guid? warehouseId)
        => CreatePurchaseOrderWithId(Guid.NewGuid(), code, vendorId, warehouseId);

    private async Task<PurchaseOrder> CreatePurchaseOrderWithId(Guid id, string code, Guid vendorId, Guid? warehouseId)
    {
        var purchaseOrderByIdGetterMock = new Mock<IGetByIdService<PurchaseOrder>>();
        purchaseOrderByIdGetterMock.Setup(getter => getter.GetByIdAsync(id)).ReturnsAsync((PurchaseOrder)null!);

        var codeCheckerMock = new Mock<ICodeExistCheckingService>();
        codeCheckerMock.Setup(checker => checker.DoesCodeExistAsync(code)).ReturnsAsync(false);

        var currentUserAccessorMock = new Mock<ICurrentUserAccessor>();
        currentUserAccessorMock.Setup(accessor => accessor.GetCurrentUserAsync()).ReturnsAsync(new CurrentUserInfoDto(Guid.NewGuid(), "username", "fullname"));

        var vendorByIdGetterMock = new Mock<IGetByIdService<Vendor>>();
        vendorByIdGetterMock.Setup(getter => getter.GetByIdAsync(vendorId)).ReturnsAsync(It.IsAny<Vendor>());

        Mock<IGetByIdService<Warehouse>> warehouseByIdGetterMock = null!;
        if (warehouseId.HasValue)
        {
            warehouseByIdGetterMock = new Mock<IGetByIdService<Warehouse>>();
            warehouseByIdGetterMock.Setup(getter => getter.GetByIdAsync(warehouseId.Value)).ReturnsAsync(It.IsAny<Warehouse>());
        }

        return await PurchaseOrder.CreateBuilder()
            .WithCode(code, codeCheckerMock.Object)
            .WithVendor(vendorId, vendorByIdGetterMock.Object)
            .WithWarehouse(warehouseId, warehouseByIdGetterMock.Object)
            .BuildAsync(purchaseOrderByIdGetterMock.Object, currentUserAccessorMock.Object);
    }

    #endregion

    #region CreatePurchaseOrderAsync

    [Fact]
    public async Task CreatePurchaseOrderAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => purchaseOrderManager.CreatePurchaseOrderAsync(null!));
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_CodeIsExists_ThrowsPurchaseOrderCodeExistsException()
    {
        var existingCode = "existing-code";
        var existingCodePurchaseOrder = await CreatePurchaseOrder(existingCode, default, null);
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.HasOne(existingCodePurchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
        var dto = new CreatePurchaseOrderDto
        {
            PlacedOnUtc = DateTime.UtcNow,
            Code = existingCode,
            CreatedByUserId = null,
            VendorId = Guid.NewGuid(),
            WarehouseId = null
        };

        await Assert.ThrowsAsync<PurchaseOrderCodeExistsException>(() => purchaseOrderManager.CreatePurchaseOrderAsync(dto));
        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_VendorIsNotFound_ThrowsVendorIsNotFoundException()
    {
        var notFoundVendorId = Guid.NewGuid();
        var dto = new CreatePurchaseOrderDto
        {
            PlacedOnUtc = DateTime.UtcNow,
            Code = "code",
            CreatedByUserId = null,
            VendorId = notFoundVendorId,
            WarehouseId = null
        };
        var vendorDataReaderMock = VendorDataReader.NotFound(notFoundVendorId);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.Empty();
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, vendorDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<VendorIsNotFoundException>(() => purchaseOrderManager.CreatePurchaseOrderAsync(dto));
        vendorDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_WarehouseIsNotFound_ThrowsWarehouseIsNotFoundException()
    {
        var notFoundWarehouseId = Guid.NewGuid();
        var dto = new CreatePurchaseOrderDto
        {
            PlacedOnUtc = DateTime.UtcNow,
            Code = "code",
            CreatedByUserId = null,
            VendorId = Guid.NewGuid(),
            WarehouseId = notFoundWarehouseId
        };
        var vendorDataReaderStub = VendorDataReader.VendorById(dto.VendorId, new Vendor(dto.VendorId, "vendor", "phone"));
        var warehouseDataReaderMock = WarehouseDataReader.NotFound(notFoundWarehouseId);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.Empty();
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, vendorDataReaderStub.Object, warehouseDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<WarehouseIsNotFoundException>(() => purchaseOrderManager.CreatePurchaseOrderAsync(dto));
        warehouseDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_CreatedByUserIsNotFound_ThrowsUserIsNotFoundException()
    {
        var notFoundCreatedByUserId = Guid.NewGuid();
        var dto = new CreatePurchaseOrderDto
        {
            PlacedOnUtc = DateTime.UtcNow,
            Code = "code",
            CreatedByUserId = notFoundCreatedByUserId,
            VendorId = Guid.NewGuid(),
            WarehouseId = null
        };
        var vendorDataReaderStub = VendorDataReader.VendorById(dto.VendorId, new Vendor(dto.VendorId, "vendor", "phone"));
        var userDataReaderMock = UserDataReader.NotFound(notFoundCreatedByUserId);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.Empty();
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, vendorDataReaderStub.Object, null!, userDataReaderMock.Object, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<UserIsNotFoundException>(() => purchaseOrderManager.CreatePurchaseOrderAsync(dto));
        userDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_DtoIsInvalid_ThrowsPurchaseOrderDataInvalidException()
    {
        var dto = new CreatePurchaseOrderDto
        {
            PlacedOnUtc = default,
            VendorId = default,
            Code = string.Empty,
            ExpectedDeliveryDateUtc = DateTime.UtcNow.AddDays(-1),
            TaxAmount = -1,
            ShippingAmount = -1,
            CreatedByUserId = null,
            WarehouseId = null
        };
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderDataIsInvalidException>(() => purchaseOrderManager.CreatePurchaseOrderAsync(dto));
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_CreatePurchaseOrder()
    {
        var warehouse = new Warehouse("warehouse-code", "warehouseName", WarehouseType.Main);
        var purchaseOrder = await CreatePurchaseOrder("code", default, warehouse.Id);
        purchaseOrder.ExpectedDeliveryDateUtc = DateTime.UtcNow.AddDays(1);
        purchaseOrder.TaxAmount = 1;
        purchaseOrder.ShippingAmount = 1;
        purchaseOrder.Note = "note";
        var vendorDataReaderStub = VendorDataReader.VendorById(purchaseOrder.VendorId, new Vendor(purchaseOrder.VendorId, "vendor", "vendor-phone"));
        var userDataReaderStub = UserDataReader.UserById(purchaseOrder.CreatedByUserId!.Value, new User(purchaseOrder.CreatedByUserId.Value, "username", "fullName", "phoneNumber"));
        var warehouseDataReaderStub = WarehouseDataReader.WarehouseById(warehouse.Id, warehouse);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.Empty();
        var purchaseOrderRepositoryMock = PurchaseOrderRepository.CreatePurchaseOrderWillReturns(purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryMock.Object, purchaseOrderDataReaderStub.Object,
            null!, vendorDataReaderStub.Object, warehouseDataReaderStub.Object, userDataReaderStub.Object, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);
        var dto = new CreatePurchaseOrderDto
        {
            PlacedOnUtc = DateTime.UtcNow,
            Code = purchaseOrder.Code,
            CreatedByUserId = purchaseOrder.CreatedByUserId,
            VendorId = purchaseOrder.VendorId,
            WarehouseId = warehouse.Id,
            Note = purchaseOrder.Note,
            ExpectedDeliveryDateUtc = purchaseOrder.ExpectedDeliveryDateUtc,
            ShippingAmount = purchaseOrder.ShippingAmount,
            TaxAmount = purchaseOrder.TaxAmount
        };

        var insertResult = await purchaseOrderManager.CreatePurchaseOrderAsync(dto);

        Assert.Equal(purchaseOrder.Id, insertResult.CreatedId);
        purchaseOrderRepositoryMock.Verify();
    }

    #endregion

    #region AddPurchaseOrderItemAsync

    [Fact]
    public async Task AddPurchaseOrderItemAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => purchaseOrderManager.AddPurchaseOrderItemAsync(null!));
    }

    [Fact]
    public async Task AddPurchaseOrderItemAsync_DtoIsInvalid_ThrowsPurchaseOrderItemDataIsInvalidException()
    {
        var invalidDataDto = new AddPurchaseOrderItemDto
        {
            ProductId = default,
            PurchaseOrderId = default,
            QuantityOrdered = 0,
            UnitCost = -1
        };
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderItemDataIsInvalidException>(() => purchaseOrderManager.AddPurchaseOrderItemAsync(invalidDataDto));
    }

    [Fact]
    public async Task AddPurchaseOrderItemAsync_PurchaseOrderIsNotFound_ThrowsPurchaseOrderIsNotFoundException()
    {
        var notFoundPurchaseOrderId = Guid.NewGuid();
        var dto = new AddPurchaseOrderItemDto
        {
            ProductId = default,
            PurchaseOrderId = notFoundPurchaseOrderId,
            QuantityOrdered = 1
        };
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.NotFound(notFoundPurchaseOrderId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.AddPurchaseOrderItemAsync(dto));

        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task AddPurchaseOrderItemAsync_ProductIsNotFound_ThrowsPurchaseOrderIsNotFoundException()
    {
        var notFoundProductId = Guid.NewGuid();
        var dto = new AddPurchaseOrderItemDto
        {
            ProductId = notFoundProductId,
            PurchaseOrderId = Guid.NewGuid(),
            QuantityOrdered = 1
        };
        var productDataReaderMock = ProductDataReader.NotFound(notFoundProductId);
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(dto.PurchaseOrderId, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, productDataReaderMock.Object, null!, null!, null!, null!, null);

        await Assert.ThrowsAsync<ProductIsNotFoundException>(() => purchaseOrderManager.AddPurchaseOrderItemAsync(dto));

        productDataReaderMock.Verify();
    }

    [Fact]
    public async Task AddPurchaseOrderItemAsync_PurchaseOrderStatusIsNotDraft_ThrowsPurchaseOrderCannotAddItemException()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var addedProductId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(addedProductId, new Product(addedProductId, "added-item-product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, addedProductId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        var dto = new AddPurchaseOrderItemDto
        {
            ProductId = Guid.NewGuid(),
            PurchaseOrderId = Guid.NewGuid(),
            QuantityOrdered = 1
        };
        productDataReaderStub = ProductDataReader.ProductById(dto.ProductId, new Product(dto.ProductId, "product"));
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(dto.PurchaseOrderId, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, productDataReaderStub.Object, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderCannotAddItemException>(() => purchaseOrderManager.AddPurchaseOrderItemAsync(dto));
    }

    [Fact]
    public async Task AddPurchaseOrderItemAsync_AddPurchaseOrderItem()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var dto = new AddPurchaseOrderItemDto
        {
            ProductId = Guid.NewGuid(),
            PurchaseOrderId = purchaseOrder.Id,
            QuantityOrdered = 1
        };
        var productDataReaderStub = ProductDataReader.ProductById(dto.ProductId, new Product(dto.ProductId, "product"));
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(dto.PurchaseOrderId, purchaseOrder);
        var purchaseOrderRepositoryMock = PurchaseOrderRepository.UpdatePurchaseOrderWillReturns(purchaseOrder);
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, dto.ProductId, 1000, 1001)
        {
            Note = "note"
        }, productDataReaderStub.Object);
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryMock.Object, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, productDataReaderStub.Object, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        var _ = await purchaseOrderManager.AddPurchaseOrderItemAsync(dto);

        purchaseOrderRepositoryMock.Verify();
    }

    #endregion

    #region ChangeStatusAsync

    [Fact]
    public async Task ChangeStatusAsync_PurchaseOrderIsNotFound_ThrowsPurchaseOrderIsNotFoundException()
    {
        var notFoundPurchaseOrderId = Guid.NewGuid();
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.NotFound(notFoundPurchaseOrderId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.ChangeStatusAsync(notFoundPurchaseOrderId, PurchaseOrderStatus.Draft));

        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task ChangeStatusAsync_ItemsIsEmpty_CannotChangeStatusFromDraftToSubmitted()
    {
        var fromStatus = PurchaseOrderStatus.Draft;
        var toStatus = PurchaseOrderStatus.Submitted;
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        purchaseOrder.ChangeStatus(fromStatus);
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderCannotChangeStatusException>(() => purchaseOrderManager.ChangeStatusAsync(purchaseOrder.Id, toStatus));
    }

    [Fact]
    public async Task ChangeStatusAsync_ItemsIsNotEmpty_ChangeStatusFromDraftToSubmitted()
    {
        var draftStatus = PurchaseOrderStatus.Draft;
        var submittedStatus = PurchaseOrderStatus.Submitted;

        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(draftStatus);

        var returnPurchaseOrder = await CreatePurchaseOrder("code", default, null);
        await returnPurchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(returnPurchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        returnPurchaseOrder.ChangeStatus(submittedStatus);

        var purchaseOrderRepositoryMock = PurchaseOrderRepository.UpdatePurchaseOrderWillReturns(returnPurchaseOrder);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryMock.Object, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        await purchaseOrderManager.ChangeStatusAsync(purchaseOrder.Id, submittedStatus);

        purchaseOrderRepositoryMock.Verify();
    }


    [Fact]
    public async Task ChangeStatusAsync_PurchaseIsCompleted_CannotChangeStatus()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Completed);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderCannotChangeStatusException>(() => purchaseOrderManager.ChangeStatusAsync(purchaseOrder.Id, PurchaseOrderStatus.Cancelled));
    }

    [Fact]
    public async Task ChangeStatusAsync_PurchaseIsCancelled_CannotChangeStatus()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Receiving);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Cancelled);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderCannotChangeStatusException>(() => purchaseOrderManager.ChangeStatusAsync(purchaseOrder.Id, PurchaseOrderStatus.Completed));
    }

    #endregion

    #region CanChangeStatusToAsync

    [Fact]
    public async Task CanChangeStatusToAsync_PurchaseOrderIsNotFound_ThrowsPurchaseOrderIsNotFoundException()
    {
        var notFoundPurchaseOrderId = Guid.NewGuid();
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.NotFound(notFoundPurchaseOrderId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.CanChangeStatusToAsync(notFoundPurchaseOrderId, PurchaseOrderStatus.Draft));

        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task CanChangeStatusToAsync_ItemsIsEmpty_CannotChangeStatusFromDraftToSubmitted_ReturnsFalse()
    {
        var fromStatus = PurchaseOrderStatus.Draft;
        var toStatus = PurchaseOrderStatus.Submitted;
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        purchaseOrder.ChangeStatus(fromStatus);
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        var falseResult = await purchaseOrderManager.CanChangeStatusToAsync(purchaseOrder.Id, toStatus);

        Assert.False(falseResult);
    }

    [Fact]
    public async Task CanChangeStatusToAsync_ItemsIsNotEmpty_ChangeStatusFromDraftToSubmitted_ReturnsTrue()
    {
        var draftStatus = PurchaseOrderStatus.Draft;
        var submittedStatus = PurchaseOrderStatus.Submitted;

        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(draftStatus);

        var returnPurchaseOrder = await CreatePurchaseOrder("code", default, null);
        await returnPurchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(returnPurchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        returnPurchaseOrder.ChangeStatus(submittedStatus);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        var trueResult = await purchaseOrderManager.CanChangeStatusToAsync(purchaseOrder.Id, submittedStatus);

        Assert.True(trueResult);
    }


    [Fact]
    public async Task CanChangeStatusToAsync_PurchaseIsCompleted_ReturnFalse()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Completed);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        var falseResult = await purchaseOrderManager.CanChangeStatusToAsync(purchaseOrder.Id, PurchaseOrderStatus.Cancelled);

        Assert.False(falseResult);
    }

    [Fact]
    public async Task CanChangeStatusToAsync_PurchaseIsCancelled_CannotChangeStatus()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Receiving);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Cancelled);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        var falseResult = await purchaseOrderManager.CanChangeStatusToAsync(purchaseOrder.Id, PurchaseOrderStatus.Completed);

        Assert.False(falseResult);
    }

    #endregion

    #region CanAddPurchaseOrderItemsAsync

    [Fact]
    public async Task CanAddPurchaseOrderItemsAsync_PurchaseOrderIsNotFound_ThrowsPurchaseOrderIsNotFoundException()
    {
        var notFoundPurchaseOrderId = Guid.NewGuid();
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.NotFound(notFoundPurchaseOrderId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.CanAddPurchaseOrderItemsAsync(notFoundPurchaseOrderId));

        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task CanAddPurchaseOrderItemsAsync_StatusNotIsDraft_ReturnsFalse()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        var falseResult = await purchaseOrderManager.CanAddPurchaseOrderItemsAsync(purchaseOrder.Id);

        Assert.False(falseResult);
    }


    [Fact]
    public async Task CanAddPurchaseOrderItemsAsync_StatusIsDraft_ReturnsTrue()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Draft);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        var falseResult = await purchaseOrderManager.CanAddPurchaseOrderItemsAsync(purchaseOrder.Id);

        Assert.True(falseResult);
    }

    #endregion

    #region CanReceiveGoodsAsync

    [Fact]
    public async Task CanReceiveGoodsAsync_PurchaseOrderIsNotFound_ThrowsPurchaseOrderIsNotFoundException()
    {
        var notFoundPurchaseOrderId = Guid.NewGuid();
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.NotFound(notFoundPurchaseOrderId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.CanReceiveGoodsAsync(notFoundPurchaseOrderId));

        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task CanReceiveGoodsAsync_StatusNotApprovedOrReceiving_ReturnsFalse()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        var falseResult = await purchaseOrderManager.CanReceiveGoodsAsync(purchaseOrder.Id);

        Assert.False(falseResult);
    }

    [Fact]
    public async Task CanReceiveGoodsAsync_StatusIsApproved_ReturnsTrue()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        var trueResult = await purchaseOrderManager.CanReceiveGoodsAsync(purchaseOrder.Id);

        Assert.True(trueResult);
    }

    [Fact]
    public async Task CanReceiveGoodsAsync_StatusIsReceiving_ReturnsTrue()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Receiving);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        var trueResult = await purchaseOrderManager.CanReceiveGoodsAsync(purchaseOrder.Id);

        Assert.True(trueResult);
    }

    #endregion

    #region DoesCodeExistAsync

    [Fact]
    public async Task DoesCodeExistAsync_ExistsAndNotCompareIds_ReturnTrue()
    {
        var existingCode = "exist-code";
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.HasOne(await CreatePurchaseOrder(existingCode, default, null));
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        var trueResult = await purchaseOrderManager.DoesCodeExistAsync(existingCode, comparesWithCurrentId: null);

        Assert.True(trueResult);
        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task DoesCodeExistAsync_ExistsAndCompareIdsIsSame_ReturnFalse()
    {
        var existingCode = "exist-code";
        var purchaseOrder = await CreatePurchaseOrder(existingCode, default, null);
        var existingId = purchaseOrder.Id;
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.HasOne(purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        var falseResult = await purchaseOrderManager.DoesCodeExistAsync(existingCode, existingId);

        Assert.False(falseResult);
        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task DoesCodeExistAsync_NotExists_ReturnFalse()
    {
        var notExistsCode = "not-exist-code";
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.Empty();
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        var falseResult = await purchaseOrderManager.DoesCodeExistAsync(notExistsCode);

        Assert.False(falseResult);
        purchaseOrderDataReaderMock.Verify();
    }

    #endregion

    #region ReceiveItemsAsync

    [Fact]
    public async Task ReceiveItemsAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => purchaseOrderManager.ReceiveItemsAsync(null!));
    }

    [Fact]
    public async Task ReceiveItemsAsync_DtoIsInvalid_ThrowsInvalidOperationException()
    {
        var invalidDto = new ReceivedGoodsForItemDto(Guid.NewGuid(), Guid.NewGuid())
        {
            WarehouseId = null,
            ReceivedQuantity = 0
        };
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<InvalidOperationException>(() => purchaseOrderManager.ReceiveItemsAsync(invalidDto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_PurchaseOrderIsNotFound_ThrowsPurchaseOrderIsNotFoundException()
    {
        var notFoundPurchaseOrderId = Guid.NewGuid();
        var dto = new ReceivedGoodsForItemDto(notFoundPurchaseOrderId, Guid.NewGuid())
        {
            WarehouseId = null,
            ReceivedQuantity = 1
        };
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.NotFound(notFoundPurchaseOrderId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_StatusIsNotApprovedOrReceiving_ThrowsPurchaseOrderCannotReceiveGoodsException()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);

        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, Guid.NewGuid())
        {
            WarehouseId = null,
            ReceivedQuantity = 1
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderCannotReceiveGoodsException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_PurchaseOrderItemsIsNotFound_ThrowsPurchaseOrderItemIsNotFoundException()
    {
        var notFoundOrderItemId = Guid.NewGuid();
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, notFoundOrderItemId)
        {
            WarehouseId = null,
            ReceivedQuantity = 1
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderItemIsNotFoundException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_ReceivedQuantityIsTooMuch_ThrowsPurchaseOrderReceiveQuantityExceedsOrderedQuantityException()
    {
        var orderedQuantity = 100;
        var receivedQuantity = 99;
        var wrongReceivingQuantity = 2;
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var product = new Product(Guid.NewGuid(), "product");
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, orderedQuantity, 0);
        purchaseOrderItem.AddQuantityReceived(receivedQuantity);
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            WarehouseId = null,
            ReceivedQuantity = wrongReceivingQuantity
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderReceiveQuantityExceedsOrderedQuantityException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_ProductIsNotFound_ThrowsProductIsNotFoundException()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var product = new Product(Guid.NewGuid(), "product");
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0);
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            WarehouseId = null,
            ReceivedQuantity = 1
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var productDataReaderMock = ProductDataReader.NotFound(product.Id);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, productDataReaderMock.Object, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ProductIsNotFoundException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_ProductTrackedAndWarehouseIdNotHaveValue_ThrowsArgumentException()
    {

        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var product = new Product(Guid.NewGuid(), "product");
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0);
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            ReceivedQuantity = 1,
            WarehouseId = null
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, productDataReaderStub.Object, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_ProductTrackedAndWarehouseIsNotFound_ThrowsWarehouseIsNotFoundException()
    {
        var notFoundWarehouseId = Guid.NewGuid();
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var product = new Product(Guid.NewGuid(), "product");
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0);
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            ReceivedQuantity = 1,
            WarehouseId = notFoundWarehouseId
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var warehouseDataReaderMock = WarehouseDataReader.NotFound(notFoundWarehouseId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, warehouseDataReaderMock.Object, null!, productDataReaderStub.Object, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<WarehouseIsNotFoundException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));

        warehouseDataReaderMock.Verify();
    }

    [Fact]
    public async Task ReceiveItemsAsync_UpdatePurchaseOrder()
    {
        var warehouseId = Guid.NewGuid();
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var product = new Product(Guid.NewGuid(), "product");
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, 100, 0);
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            ReceivedQuantity = 100,
            WarehouseId = null
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var warehouseDataReaderMock = WarehouseDataReader.WarehouseById(warehouseId, new Warehouse("code", "warehouse", WarehouseType.SubWarehouse));
        var purchaseOrderRepositoryMock = PurchaseOrderRepository.UpdatePurchaseOrderWillReturns(purchaseOrder);
        var stockDataReaderStub = EntityDataReader.Create<InventoryStock>().WithData(Array.Empty<InventoryStock>());
        var productRepositoryStub = new Mock<IRepository<Product>>();
        productRepositoryStub.Setup(r => r.UpdateAsync(It.IsAny<Product>(), default)).ReturnsAsync((Product p, CancellationToken _) => p);
        var priceHistoryRepositoryStub = new Mock<IRepository<ProductPriceHistory>>();
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryMock.Object, purchaseOrderDataReaderStub.Object, null!, null!, warehouseDataReaderMock.Object, null!, productDataReaderStub.Object, Mock.Of<IEventPublisher>(), stockDataReaderStub.Object, productRepositoryStub.Object, priceHistoryRepositoryStub.Object, null!);

        var receiveItemResult = await purchaseOrderManager.ReceiveItemsAsync(dto);

        Assert.Equal(dto.ReceivedQuantity, receiveItemResult.ReceivedQuantity);
        purchaseOrderRepositoryMock.Verify();
    }

    [Fact]
    public async Task ReceiveItemsAsync_ProductIsTrackedInventory_AddStockReveive()
    {
        var warehouseId = Guid.NewGuid();
        var purchaseOrder = await CreatePurchaseOrder("code", default, warehouseId);
        var product = new Product(Guid.NewGuid(), "product");
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, 100, 0);
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            ReceivedQuantity = 100,
            WarehouseId = null
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var warehouseDataReaderMock = WarehouseDataReader.WarehouseById(warehouseId, new Warehouse("code", "warehouse", WarehouseType.SubWarehouse));
        var purchaseOrderRepositoryStub = PurchaseOrderRepository.UpdatePurchaseOrderWillReturns(purchaseOrder);
        var inventoryStockManagerMock = new Mock<IInventoryStockManager>();
        inventoryStockManagerMock.Setup(service => service.ReceiveStockAsync(
            product.Id, warehouseId, dto.ReceivedQuantity, It.IsAny<string>(), It.IsAny<Guid?>(), (int)StockReferenceType.PurchaseOrder, purchaseOrder.Id));
        var stockDataReaderStub = EntityDataReader.Create<InventoryStock>().WithData(Array.Empty<InventoryStock>());
        var productRepositoryStub = new Mock<IRepository<Product>>();
        productRepositoryStub.Setup(r => r.UpdateAsync(It.IsAny<Product>(), default)).ReturnsAsync((Product p, CancellationToken _) => p);
        var priceHistoryRepositoryStub = new Mock<IRepository<ProductPriceHistory>>();
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryStub.Object, purchaseOrderDataReaderStub.Object, inventoryStockManagerMock.Object, null!, warehouseDataReaderMock.Object, null!, productDataReaderStub.Object, Mock.Of<IEventPublisher>(),
                stockDataReaderStub.Object, productRepositoryStub.Object, priceHistoryRepositoryStub.Object, null!);

        var receiveItemResult = await purchaseOrderManager.ReceiveItemsAsync(dto);

        Assert.Equal(dto.ReceivedQuantity, receiveItemResult.ReceivedQuantity);
        inventoryStockManagerMock.Verify();
    }

    [Fact]
    public async Task ReceiveItemsAsync_SellingPriceIsNull_KeepsProductUnitPrice()
    {
        var warehouseId = Guid.NewGuid();
        var purchaseOrder = await CreatePurchaseOrder("code", default, warehouseId);
        var product = new Product(Guid.NewGuid(), "product");
        product.UpdatePrice(unitPrice: 200, costPrice: 80);
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, 100, unitCost: 90);
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            WarehouseId = null,
            ReceivedQuantity = 100,
            SellingPrice = null // không nhập giá bán
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var warehouseDataReaderStub = WarehouseDataReader.WarehouseById(warehouseId, new Warehouse("code", "warehouse", WarehouseType.SubWarehouse));
        var purchaseOrderRepositoryStub = PurchaseOrderRepository.UpdatePurchaseOrderWillReturns(purchaseOrder);
        var stockDataReaderStub = EntityDataReader.Create<InventoryStock>().WithData(Array.Empty<InventoryStock>());
        var productRepositoryMock = new Mock<IRepository<Product>>();
        productRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Product>(), default)).ReturnsAsync((Product p, CancellationToken _) => p);
        var priceHistoryRepositoryMock = new Mock<IRepository<ProductPriceHistory>>();

        var purchaseOrderManager = new PurchaseOrderManager(
            purchaseOrderRepositoryStub.Object, purchaseOrderDataReaderStub.Object, null!, null!,
            warehouseDataReaderStub.Object, null!, productDataReaderStub.Object, Mock.Of<IEventPublisher>(),
            stockDataReaderStub.Object, productRepositoryMock.Object, priceHistoryRepositoryMock.Object, null!);

        await purchaseOrderManager.ReceiveItemsAsync(dto);

        // UnitPrice giữ nguyên 200 vì không truyền SellingPrice
        Assert.Equal(200m, product.UnitPrice);
        // CostPrice cập nhật theo weighted avg = (0*0 + 100*90)/100 = 90
        Assert.Equal(90m, product.CostPrice);
        // Vì CostPrice có thay đổi (80 -> 90) thì vẫn insert history
        priceHistoryRepositoryMock.Verify(r => r.InsertAsync(
            It.Is<ProductPriceHistory>(h => h.ProductId == product.Id
                && h.OldPrice == 200m && h.NewPrice == 200m
                && h.OldCostPrice == 80m && h.NewCostPrice == 90m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveItemsAsync_SellingPriceIsProvided_UpdatesProductUnitPriceAndInsertsHistory()
    {
        var warehouseId = Guid.NewGuid();
        var purchaseOrder = await CreatePurchaseOrder("PO-001", default, warehouseId);
        var product = new Product(Guid.NewGuid(), "product");
        product.UpdatePrice(unitPrice: 150, costPrice: 100);
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, 50, unitCost: 120);
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            WarehouseId = null,
            ReceivedQuantity = 50,
            SellingPrice = 180
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var warehouseDataReaderStub = WarehouseDataReader.WarehouseById(warehouseId, new Warehouse("code", "warehouse", WarehouseType.SubWarehouse));
        var purchaseOrderRepositoryStub = PurchaseOrderRepository.UpdatePurchaseOrderWillReturns(purchaseOrder);
        var stockDataReaderStub = EntityDataReader.Create<InventoryStock>().WithData(Array.Empty<InventoryStock>());
        var productRepositoryMock = new Mock<IRepository<Product>>();
        productRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Product>(), default)).ReturnsAsync((Product p, CancellationToken _) => p);
        var priceHistoryRepositoryMock = new Mock<IRepository<ProductPriceHistory>>();

        var purchaseOrderManager = new PurchaseOrderManager(
            purchaseOrderRepositoryStub.Object, purchaseOrderDataReaderStub.Object, null!, null!,
            warehouseDataReaderStub.Object, null!, productDataReaderStub.Object, Mock.Of<IEventPublisher>(),
            stockDataReaderStub.Object, productRepositoryMock.Object, priceHistoryRepositoryMock.Object, null!);

        await purchaseOrderManager.ReceiveItemsAsync(dto);

        Assert.Equal(180m, product.UnitPrice);
        Assert.Equal(120m, product.CostPrice);
        productRepositoryMock.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        priceHistoryRepositoryMock.Verify(r => r.InsertAsync(
            It.Is<ProductPriceHistory>(h => h.ProductId == product.Id
                && h.OldPrice == 150m && h.NewPrice == 180m
                && h.OldCostPrice == 100m && h.NewCostPrice == 120m
                && h.Note.Contains("PO-001")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveItemsAsync_SellingPriceIsNegative_ThrowsInvalidOperationException()
    {
        var dto = new ReceivedGoodsForItemDto(Guid.NewGuid(), Guid.NewGuid())
        {
            WarehouseId = null,
            ReceivedQuantity = 10,
            SellingPrice = -1
        };
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<InvalidOperationException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_SellingPriceEqualsCurrentUnitPriceAndCostPriceUnchanged_DoesNotInsertHistory()
    {
        var warehouseId = Guid.NewGuid();
        var purchaseOrder = await CreatePurchaseOrder("code", default, warehouseId);
        var product = new Product(Guid.NewGuid(), "product");
        product.UpdatePrice(unitPrice: 200, costPrice: 100);
        // existing stock = 100 đơn vị với costPrice 100 → weighted avg giữ nguyên khi nhập tiếp 100 đơn vị với cost 100
        var existingStock = new InventoryStock(Guid.NewGuid(), product.Id, warehouseId, default)
        {
            QuantityOnHand = 100
        };
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, 100, unitCost: 100);
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            WarehouseId = null,
            ReceivedQuantity = 100,
            SellingPrice = 200 // bằng giá bán hiện tại
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var warehouseDataReaderStub = WarehouseDataReader.WarehouseById(warehouseId, new Warehouse("code", "warehouse", WarehouseType.SubWarehouse));
        var purchaseOrderRepositoryStub = PurchaseOrderRepository.UpdatePurchaseOrderWillReturns(purchaseOrder);
        var stockDataReaderStub = EntityDataReader.Create<InventoryStock>().WithData(existingStock);
        var productRepositoryMock = new Mock<IRepository<Product>>();
        productRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Product>(), default)).ReturnsAsync((Product p, CancellationToken _) => p);
        var priceHistoryRepositoryMock = new Mock<IRepository<ProductPriceHistory>>();

        var purchaseOrderManager = new PurchaseOrderManager(
            purchaseOrderRepositoryStub.Object, purchaseOrderDataReaderStub.Object, null!, null!,
            warehouseDataReaderStub.Object, null!, productDataReaderStub.Object, Mock.Of<IEventPublisher>(),
            stockDataReaderStub.Object, productRepositoryMock.Object, priceHistoryRepositoryMock.Object, null!);

        await purchaseOrderManager.ReceiveItemsAsync(dto);

        // Không có thay đổi giá → không insert history
        priceHistoryRepositoryMock.Verify(r => r.InsertAsync(It.IsAny<ProductPriceHistory>(), It.IsAny<CancellationToken>()), Times.Never);
        productRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region VerifyStatusAsync

    [Fact]
    public async Task VerifyStatusAsync_PurchaseOrderIsNotFound_ThrowsPurchaseOrderIsNotFoundException()
    {
        var notFoundPurchaseOrderId = Guid.NewGuid();
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.NotFound(notFoundPurchaseOrderId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.VerifyStatusAsync(notFoundPurchaseOrderId));

        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task VerifyStatusAsync_StatusIsCompleted_Returns()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Completed);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderRepositoryMock = PurchaseOrderRepository.UpdatePurchaseOrderWillReturns(purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await purchaseOrderManager.VerifyStatusAsync(purchaseOrder.Id);

        purchaseOrderRepositoryMock.Verify(repository => repository.UpdateAsync(It.IsAny<PurchaseOrder>()), Times.Never);
    }

    [Fact]
    public async Task VerifyStatusAsync_StatusIsCancelled_Returns()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Cancelled);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderRepositoryMock = PurchaseOrderRepository.UpdatePurchaseOrderWillReturns(purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await purchaseOrderManager.VerifyStatusAsync(purchaseOrder.Id);

        purchaseOrderRepositoryMock.Verify(repository => repository.UpdateAsync(It.IsAny<PurchaseOrder>()), Times.Never);
    }

    [Fact]
    public async Task VerifyStatusAsync_AllItemsReceived_ChangeStatusToCompletedAndUpdate()
    {
        var orderedQuantity = 100;
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, orderedQuantity, 0);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Receiving);
        purchaseOrderItem.AddQuantityReceived(orderedQuantity);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderRepositoryMock = new Mock<IRepository<PurchaseOrder>>();
        purchaseOrderRepositoryMock.Setup(repository => repository.UpdateAsync(It.Is<PurchaseOrder>(po => po.Status == PurchaseOrderStatus.Completed)));
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryMock.Object, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await purchaseOrderManager.VerifyStatusAsync(purchaseOrder.Id);

        purchaseOrderRepositoryMock.Verify();
    }

    [Fact]
    public async Task VerifyStatusAsync_HasAnyItemsReceiveGoods_ChangeStatusToReceivingAndUpdate()
    {
        var receivedQuantity = 1;
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, 100, 0);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrderItem.AddQuantityReceived(receivedQuantity);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderRepositoryMock = new Mock<IRepository<PurchaseOrder>>();
        purchaseOrderRepositoryMock.Setup(repository => repository.UpdateAsync(It.Is<PurchaseOrder>(po => po.Status == PurchaseOrderStatus.Receiving)));
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryMock.Object, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await purchaseOrderManager.VerifyStatusAsync(purchaseOrder.Id);

        purchaseOrderRepositoryMock.Verify();
    }

    #endregion

    #region GetPurchaseOrderByIdAsync

    [Fact]
    public async Task GetPurchaseOrderByIdAsync_PurchaseOrderNotFound_ReturnsNull()
    {
        var notFoundPurchaseOrderId = Guid.NewGuid();
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.NotFound(notFoundPurchaseOrderId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        var nullResult = await purchaseOrderManager.GetPurchaseOrderByIdAsync(notFoundPurchaseOrderId);

        Assert.Null(nullResult);
        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task GetPurchaseOrderByIdAsync_PurchaseOrderIsFound_ReturnsDto()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        var dto = await purchaseOrderManager.GetPurchaseOrderByIdAsync(purchaseOrder.Id);

        Assert.Equal(purchaseOrder.Id, dto!.Id);
        Assert.Equal(purchaseOrder.Code, dto.Code);
        purchaseOrderDataReaderMock.Verify();
    }

    #endregion

    #region GetPurchaseOrdersAsync

    [Fact]
    public async Task GetPurchaseOrdersAsync_PageIndexLessThan0_ThrowsArgumentOutOfRangeException()
    {
        var invalidPageIndex = -1;
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => purchaseOrderManager.GetPurchaseOrdersAsync("", invalidPageIndex, 1));
    }

    [Fact]
    public async Task GetPurchaseOrdersAsync_PageSizeLessThanOrEqualTo0_ThrowsArgumentOutOfRangeException()
    {
        var invalidPageSize = 0;
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => purchaseOrderManager.GetPurchaseOrdersAsync("", 0, invalidPageSize));
    }

    [Fact]
    public async Task GetPurchaseOrdersAsync_KeywordsIsEmpty_ReturnsOrderByCreatedDateDescendingOrders()
    {
        var pageIndex = 0;
        var pageSize = 1;
        var data = new[]
        {
            await CreatePurchaseOrder("code-1", default, null),
            await CreatePurchaseOrder("code-2", default, null),
            await CreatePurchaseOrder("code-3", default, null),
            await CreatePurchaseOrder("code-4", default, null)
        };
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.WithData(data);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await purchaseOrderManager.GetPurchaseOrdersAsync(null, pageIndex, pageSize);

        Assert.Equal(data.Last().Id, result.First().Id);
        Assert.Equal(data.Length, result.PagerInfo.TotalCount);
        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task GetPurchaseOrdersAsync_KeywordsIsNotEmpty_ReturnsValidResult()
    {
        var keywords = "keywords";
        var pageIndex = 0;
        var pageSize = 1;
        var vendorHasKeywords = new Vendor(Guid.NewGuid(), $"{keywords}_abc", "___");
        var warehouseHasKeywords = new Warehouse("code", $"{keywords}_def", default);
        var data = new[]
        {
            await CreatePurchaseOrder("code-1", vendorHasKeywords.Id, null),
            await CreatePurchaseOrder("code-2", default, null),
            await CreatePurchaseOrder("code-3", default, warehouseHasKeywords.Id),
            await CreatePurchaseOrder("code-4", vendorHasKeywords.Id, warehouseHasKeywords.Id) 
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.WithData(data);
        var vendorDataReaderMock = VendorDataReader.WithData(vendorHasKeywords);
        var warehouseDataReaderMock = WarehouseDataReader.WithData(warehouseHasKeywords);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, vendorDataReaderMock.Object, warehouseDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!);

        var result = await purchaseOrderManager.GetPurchaseOrdersAsync(keywords, pageIndex, pageSize);

        Assert.Equal(3, result.PagerInfo.TotalCount);
        Assert.Equal(data[3].Id, result.First().Id);
        vendorDataReaderMock.Verify();
        warehouseDataReaderMock.Verify();
    }

    #endregion

    #region UpdatePurchaseOrderAsync

    [Fact]
    public async Task UpdatePurchaseOrderAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => purchaseOrderManager.UpdatePurchaseOrderAsync(null!));
    }

    [Fact]
    public async Task UpdatePurchaseOrderAsync_DtoIsInvalid_ThrowsPurchaseOrderDataIsInvalidException()
    {
        var invalidUpdatePurchaseOrderDto = new UpdatePurchaseOrderDto(default)
        {
            PlacedOnUtc = default,
            VendorId = default,
            TaxAmount = -1,
            ShippingAmount = -1,
            ExpectedDeliveryDateUtc = DateTime.UtcNow.AddDays(-1),
            WarehouseId = null
        };
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderDataIsInvalidException>(() => purchaseOrderManager.UpdatePurchaseOrderAsync(invalidUpdatePurchaseOrderDto));
    }

    [Fact]
    public async Task UpdatePurchaseOrderAsync_PurchaseOrderIsNotFound_ThrowsPurchaseOrderIsNotFoundException()
    {
        var notFoundPurchaseOrderId = Guid.NewGuid();
        var dto = new UpdatePurchaseOrderDto(notFoundPurchaseOrderId)
        {
            PlacedOnUtc = DateTime.UtcNow,
            VendorId = Guid.NewGuid(),
            WarehouseId = null
        };
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.NotFound(notFoundPurchaseOrderId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.UpdatePurchaseOrderAsync(dto));

        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdatePurchaseOrderAsync_VendorIsNotFound_ThrowsVendorIsNotFoundException()
    {
        var notFoundVendorId = Guid.NewGuid();
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var dto = new UpdatePurchaseOrderDto(purchaseOrder.Id)
        {
            PlacedOnUtc = DateTime.UtcNow,
            VendorId = notFoundVendorId,
            WarehouseId = null
        };
        var vendorDataReaderMock = VendorDataReader.NotFound(notFoundVendorId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, vendorDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<VendorIsNotFoundException>(() => purchaseOrderManager.UpdatePurchaseOrderAsync(dto));

        vendorDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdatePurchaseOrderAsync_UpdateProductOrder()
    {
        var purchaseOrder = await CreatePurchaseOrder("code", default, null);
        var vendor = new Vendor(Guid.NewGuid(), "vendor", "phone");
        var dto = new UpdatePurchaseOrderDto(purchaseOrder.Id)
        {
            PlacedOnUtc = DateTime.UtcNow,
            VendorId = vendor.Id,
            ShippingAmount = 100,
            TaxAmount = 200,
            WarehouseId = null
        };
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var vendorDataReaderStub = VendorDataReader.VendorById(vendor.Id, vendor);
        var purchaseOrderRepositoryMock = PurchaseOrderRepository.UpdatePurchaseOrderWillReturns(purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryMock.Object, purchaseOrderDataReaderMock.Object, null!, vendorDataReaderStub.Object, null!, null!, null!, Mock.Of<IEventPublisher>(), null!, null!, null!, null!);

        var updateResult = await purchaseOrderManager.UpdatePurchaseOrderAsync(dto);

        Assert.Equal(dto.ShippingAmount, updateResult.ShippingAmount);
        Assert.Equal(dto.TaxAmount, updateResult.TaxAmount);
        purchaseOrderRepositoryMock.Verify();
    }

    #endregion

    #region GetRecentPurchasePricesAsync

    [Fact]
    public async Task GetRecentPurchasePricesAsync_ProductHasNoPurchaseOrders_ReturnsEmptyList()
    {
        var productId = Guid.NewGuid();
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.Empty();
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await purchaseOrderManager.GetRecentPurchasePricesAsync(productId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecentPurchasePricesAsync_CancelledOrdersAreExcluded_ReturnsEmptyList()
    {
        // Arrange: 1 đơn bị hủy (Draft → Cancelled) có chứa sản phẩm
        var productId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        var cancelledPo = await CreatePurchaseOrder("code", vendorId, null);

        var product = new Product(productId, "prod");
        var productDataReaderStub = ProductDataReader.ProductById(productId, product);
        await cancelledPo.AddPurchaseOrderItemAsync(
            new PurchaseOrderItem(cancelledPo.Id, productId, 10, 150_000),
            productDataReaderStub.Object);
        // Hủy đơn khi chưa nhận hàng (QuantityReceived = 0 → hợp lệ)
        cancelledPo.ChangeStatus(PurchaseOrderStatus.Cancelled);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.WithData(cancelledPo);
        var vendorDataReaderStub = VendorDataReader.Empty();
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, vendorDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await purchaseOrderManager.GetRecentPurchasePricesAsync(productId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecentPurchasePricesAsync_MultipleOrdersSameVendor_ReturnsOnlyMostRecentPerVendor()
    {
        // Arrange: 2 đơn cùng NCC → chỉ trả về 1 kết quả (gần nhất)
        var productId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();

        var olderPo = await CreatePurchaseOrder("code", vendorId, null);
        var newerPo = await CreatePurchaseOrder("code-2", vendorId, null);

        var product = new Product(productId, "product");
        var productDataReaderStub = ProductDataReader.ProductById(productId, product);

        await olderPo.AddPurchaseOrderItemAsync(new PurchaseOrderItem(olderPo.Id, productId, 5, 100_000), productDataReaderStub.Object);
        await newerPo.AddPurchaseOrderItemAsync(new PurchaseOrderItem(newerPo.Id, productId, 10, 120_000), productDataReaderStub.Object);

        var vendor = new Vendor(vendorId, "NCC Minh Hòa", "0901234567");
        // newerPo được tạo sau olderPo nên CreatedOnUtc lớn hơn → phải là gần nhất
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.WithData(olderPo, newerPo);
        var vendorDataReaderStub = VendorDataReader.WithData(vendor);

        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, vendorDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await purchaseOrderManager.GetRecentPurchasePricesAsync(productId);

        Assert.Single(result);
        Assert.Equal("PO-NEWER", result[0].PurchaseOrderCode);
        Assert.Equal(120_000, result[0].UnitCost);
        Assert.Equal(vendorId, result[0].VendorId);
        Assert.Equal("NCC Minh Hòa", result[0].VendorName);
    }

    [Fact]
    public async Task GetRecentPurchasePricesAsync_MultipleVendors_ReturnsOneEntryPerVendor()
    {
        // Arrange: 2 đơn với 2 NCC khác nhau → trả về 2 kết quả
        var productId = Guid.NewGuid();
        var vendorId1 = Guid.NewGuid();
        var vendorId2 = Guid.NewGuid();

        var po1 = await CreatePurchaseOrder("code-1", vendorId1, null);
        var po2 = await CreatePurchaseOrder("code-2", vendorId2, null);

        var product = new Product(productId, "product");
        var productDataReaderStub = ProductDataReader.ProductById(productId, product);

        await po1.AddPurchaseOrderItemAsync(new PurchaseOrderItem(po1.Id, productId, 5, 150_000), productDataReaderStub.Object);
        await po2.AddPurchaseOrderItemAsync(new PurchaseOrderItem(po2.Id, productId, 3, 145_000), productDataReaderStub.Object);

        var vendor1 = new Vendor(vendorId1, "NCC A", "0900000001");
        var vendor2 = new Vendor(vendorId2, "NCC B", "0900000002");
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.WithData(po1, po2);
        var vendorDataReaderStub = VendorDataReader.WithData(vendor1, vendor2);

        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, vendorDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await purchaseOrderManager.GetRecentPurchasePricesAsync(productId);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.VendorId == vendorId1 && r.UnitCost == 150_000);
        Assert.Contains(result, r => r.VendorId == vendorId2 && r.UnitCost == 145_000);
    }

    #endregion
}

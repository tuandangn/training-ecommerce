using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Services.PurchaseOrders;
using NamEcommerce.Domain.Services.Test.Helpers;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Users;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class PurchaseOrderManagerTests
{
    #region CreatePurchaseOrderAsync

    [Fact]
    public async Task CreatePurchaseOrderAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => purchaseOrderManager.CreatePurchaseOrderAsync(null!));
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_CodeIsExists_ThrowsPurchaseOrderCodeExistsException()
    {
        var existingCode = "existing-code";
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.HasOne(new PurchaseOrder(existingCode, null!, null!, null!));
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!);
        var dto = new CreatePurchaseOrderDto
        {
            Code = existingCode,
            CreatedByUserId = null,
            VendorId = null,
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
            Code = "code",
            CreatedByUserId = null,
            VendorId = notFoundVendorId,
            WarehouseId = null
        };
        var vendorDataReaderMock = VendorDataReader.NotFound(notFoundVendorId);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.Empty();
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, vendorDataReaderMock.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<VendorIsNotFoundException>(() => purchaseOrderManager.CreatePurchaseOrderAsync(dto));
        vendorDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_WarehouseIsNotFound_ThrowsWarehouseIsNotFoundException()
    {
        var notFoundWarehouseId = Guid.NewGuid();
        var dto = new CreatePurchaseOrderDto
        {
            Code = "code",
            CreatedByUserId = null,
            VendorId = null,
            WarehouseId = notFoundWarehouseId
        };
        var warehouseDataReaderMock = WarehouseDataReader.NotFound(notFoundWarehouseId);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.Empty();
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, warehouseDataReaderMock.Object, null!, null!, null!);

        await Assert.ThrowsAsync<WarehouseIsNotFoundException>(() => purchaseOrderManager.CreatePurchaseOrderAsync(dto));
        warehouseDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_CreatedByUserIsNotFound_ThrowsUserIsNotFoundException()
    {
        var notFoundCreatedByUserId = Guid.NewGuid();
        var dto = new CreatePurchaseOrderDto
        {
            Code = "code",
            CreatedByUserId = notFoundCreatedByUserId,
            VendorId = null,
            WarehouseId = null
        };
        var userDataReaderMock = UserDataReader.NotFound(notFoundCreatedByUserId);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.Empty();
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, userDataReaderMock.Object, null!, null!);

        await Assert.ThrowsAsync<UserIsNotFoundException>(() => purchaseOrderManager.CreatePurchaseOrderAsync(dto));
        userDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_DtoIsInvalid_ThrowsPurchaseOrderDataInvalidException()
    {
        var dto = new CreatePurchaseOrderDto
        {
            Code = string.Empty,
            ExpectedDeliveryDateUtc = DateTime.UtcNow.AddDays(-1),
            TaxAmount = -1,
            ShippingAmount = -1,
            CreatedByUserId = null,
            WarehouseId = null,
            VendorId = null
        };
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderDataIsInvalidException>(() => purchaseOrderManager.CreatePurchaseOrderAsync(dto));
    }

    [Fact]
    public async Task CreatePurchaseOrderAsync_CreatePurchaseOrder()
    {
        var warehouse = new Warehouse("warehouse-code", "warehouseName", WarehouseType.Main);
        var purchaseOrder = new PurchaseOrder("code", Guid.NewGuid(), warehouse.Id, Guid.NewGuid())
        {
            ExpectedDeliveryDateUtc = DateTime.UtcNow.AddDays(1),
            TaxAmount = 1,
            ShippingAmount = 1,
            Note = "note"
        };
        var vendorDataReaderStub = VendorDataReader.VendorById(purchaseOrder.VendorId!.Value, new Vendor(purchaseOrder.VendorId.Value, "vendor", "vendor-phone"));
        var userDataReaderStub = UserDataReader.UserById(purchaseOrder.CreatedByUserId!.Value, new User(purchaseOrder.CreatedByUserId.Value, "username", "fullName", "phoneNumber"));
        var warehouseDataReaderStub = WarehouseDataReader.WarehouseById(warehouse.Id, warehouse);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.Empty();
        var purchaseOrderRepositoryMock = PurchaseOrderRepository.CreatePurchaseOrderWillReturns(purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryMock.Object, purchaseOrderDataReaderStub.Object,
            null!, vendorDataReaderStub.Object, warehouseDataReaderStub.Object, userDataReaderStub.Object, null!, Mock.Of<IEventPublisher>());
        var dto = new CreatePurchaseOrderDto
        {
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
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!);

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
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!);

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
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!);

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
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(dto.PurchaseOrderId, new PurchaseOrder("code", null!, null!, null!));
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, productDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<ProductIsNotFoundException>(() => purchaseOrderManager.AddPurchaseOrderItemAsync(dto));

        productDataReaderMock.Verify();
    }

    [Fact]
    public async Task AddPurchaseOrderItemAsync_PurchaseOrderStatusIsNotDraft_ThrowsPurchaseOrderCannotAddItemException()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
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
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, productDataReaderStub.Object, null!);

        await Assert.ThrowsAsync<PurchaseOrderCannotAddItemException>(() => purchaseOrderManager.AddPurchaseOrderItemAsync(dto));
    }

    [Fact]
    public async Task AddPurchaseOrderItemAsync_AddPurchaseOrderItem()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
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
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryMock.Object, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, productDataReaderStub.Object, Mock.Of<IEventPublisher>());

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
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.ChangeStatusAsync(notFoundPurchaseOrderId, PurchaseOrderStatus.Draft));

        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task ChangeStatusAsync_ItemsIsEmpty_CannotChangeStatusFromDraftToSubmitted()
    {
        var fromStatus = PurchaseOrderStatus.Draft;
        var toStatus = PurchaseOrderStatus.Submitted;
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        purchaseOrder.ChangeStatus(fromStatus);
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderCannotChangeStatusException>(() => purchaseOrderManager.ChangeStatusAsync(purchaseOrder.Id, toStatus));
    }

    [Fact]
    public async Task ChangeStatusAsync_ItemsIsNotEmpty_ChangeStatusFromDraftToSubmitted()
    {
        var draftStatus = PurchaseOrderStatus.Draft;
        var submittedStatus = PurchaseOrderStatus.Submitted;

        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(draftStatus);

        var returnPurchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        await returnPurchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(returnPurchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        returnPurchaseOrder.ChangeStatus(submittedStatus);

        var purchaseOrderRepositoryMock = PurchaseOrderRepository.UpdatePurchaseOrderWillReturns(returnPurchaseOrder);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryMock.Object, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        await purchaseOrderManager.ChangeStatusAsync(purchaseOrder.Id, submittedStatus);

        purchaseOrderRepositoryMock.Verify();
    }


    [Fact]
    public async Task ChangeStatusAsync_PurchaseIsCompleted_CannotChangeStatus()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Completed);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        await Assert.ThrowsAsync<PurchaseOrderCannotChangeStatusException>(() => purchaseOrderManager.ChangeStatusAsync(purchaseOrder.Id, PurchaseOrderStatus.Cancelled));
    }

    [Fact]
    public async Task ChangeStatusAsync_PurchaseIsCancelled_CannotChangeStatus()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Receiving);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Cancelled);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        await Assert.ThrowsAsync<PurchaseOrderCannotChangeStatusException>(() => purchaseOrderManager.ChangeStatusAsync(purchaseOrder.Id, PurchaseOrderStatus.Completed));
    }

    #endregion

    #region CanChangeStatusToAsync

    [Fact]
    public async Task CanChangeStatusToAsync_PurchaseOrderIsNotFound_ThrowsPurchaseOrderIsNotFoundException()
    {
        var notFoundPurchaseOrderId = Guid.NewGuid();
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.NotFound(notFoundPurchaseOrderId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.CanChangeStatusToAsync(notFoundPurchaseOrderId, PurchaseOrderStatus.Draft));

        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task CanChangeStatusToAsync_ItemsIsEmpty_CannotChangeStatusFromDraftToSubmitted_ReturnsFalse()
    {
        var fromStatus = PurchaseOrderStatus.Draft;
        var toStatus = PurchaseOrderStatus.Submitted;
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        purchaseOrder.ChangeStatus(fromStatus);
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!);

        var falseResult = await purchaseOrderManager.CanChangeStatusToAsync(purchaseOrder.Id, toStatus);

        Assert.False(falseResult);
    }

    [Fact]
    public async Task CanChangeStatusToAsync_ItemsIsNotEmpty_ChangeStatusFromDraftToSubmitted_ReturnsTrue()
    {
        var draftStatus = PurchaseOrderStatus.Draft;
        var submittedStatus = PurchaseOrderStatus.Submitted;

        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(draftStatus);

        var returnPurchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        await returnPurchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(returnPurchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        returnPurchaseOrder.ChangeStatus(submittedStatus);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        var trueResult = await purchaseOrderManager.CanChangeStatusToAsync(purchaseOrder.Id, submittedStatus);

        Assert.True(trueResult);
    }


    [Fact]
    public async Task CanChangeStatusToAsync_PurchaseIsCompleted_ReturnFalse()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Completed);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        var falseResult = await purchaseOrderManager.CanChangeStatusToAsync(purchaseOrder.Id, PurchaseOrderStatus.Cancelled);

        Assert.False(falseResult);
    }

    [Fact]
    public async Task CanChangeStatusToAsync_PurchaseIsCancelled_CannotChangeStatus()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Receiving);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Cancelled);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

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
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.CanAddPurchaseOrderItemsAsync(notFoundPurchaseOrderId));

        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task CanAddPurchaseOrderItemsAsync_StatusNotIsDraft_ReturnsFalse()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        var falseResult = await purchaseOrderManager.CanAddPurchaseOrderItemsAsync(purchaseOrder.Id);

        Assert.False(falseResult);
    }


    [Fact]
    public async Task CanAddPurchaseOrderItemsAsync_StatusIsDraft_ReturnsTrue()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Draft);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

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
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.CanReceiveGoodsAsync(notFoundPurchaseOrderId));

        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task CanReceiveGoodsAsync_StatusNotApprovedOrReceiving_ReturnsFalse()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var productId = Guid.NewGuid();
        var productDataReaderStub = ProductDataReader.ProductById(productId, new Product(productId, "product"));
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, productId, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        var falseResult = await purchaseOrderManager.CanReceiveGoodsAsync(purchaseOrder.Id);

        Assert.False(falseResult);
    }

    [Fact]
    public async Task CanReceiveGoodsAsync_StatusIsApproved_ReturnsTrue()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        var trueResult = await purchaseOrderManager.CanReceiveGoodsAsync(purchaseOrder.Id);

        Assert.True(trueResult);
    }

    [Fact]
    public async Task CanReceiveGoodsAsync_StatusIsReceiving_ReturnsTrue()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Receiving);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        var trueResult = await purchaseOrderManager.CanReceiveGoodsAsync(purchaseOrder.Id);

        Assert.True(trueResult);
    }

    #endregion

    #region DoesCodeExistAsync

    [Fact]
    public async Task DoesCodeExistAsync_ExistsAndNotCompareIds_ReturnTrue()
    {
        var existingCode = "exist-code";
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.HasOne(new PurchaseOrder(existingCode, null!, null!, null!));
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        var trueResult = await purchaseOrderManager.DoesCodeExistAsync(existingCode, comparesWithCurrentId: null);

        Assert.True(trueResult);
        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task DoesCodeExistAsync_ExistsAndCompareIdsIsSame_ReturnFalse()
    {
        var existingCode = "exist-code";
        var purchaseOrder = new PurchaseOrder(existingCode, null!, null!, null!);
        var existingId = purchaseOrder.Id;
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.HasOne(purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        var falseResult = await purchaseOrderManager.DoesCodeExistAsync(existingCode, existingId);

        Assert.False(falseResult);
        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task DoesCodeExistAsync_NotExists_ReturnFalse()
    {
        var notExistsCode = "not-exist-code";
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.Empty();
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        var falseResult = await purchaseOrderManager.DoesCodeExistAsync(notExistsCode);

        Assert.False(falseResult);
        purchaseOrderDataReaderMock.Verify();
    }

    #endregion

    #region ReceiveItemsAsync

    [Fact]
    public async Task ReceiveItemsAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => purchaseOrderManager.ReceiveItemsAsync(null!));
    }

    [Fact]
    public async Task ReceiveItemsAsync_DtoIsInvalid_ThrowsInvalidOperationException()
    {
        var invalidDto = new ReceivedGoodsForItemDto(Guid.NewGuid(), Guid.NewGuid())
        {
            ReceivedQuantity = 0
        };
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<InvalidOperationException>(() => purchaseOrderManager.ReceiveItemsAsync(invalidDto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_PurchaseOrderIsNotFound_ThrowsPurchaseOrderIsNotFoundException()
    {
        var notFoundPurchaseOrderId = Guid.NewGuid();
        var dto = new ReceivedGoodsForItemDto(notFoundPurchaseOrderId, Guid.NewGuid())
        {
            ReceivedQuantity = 1
        };
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.NotFound(notFoundPurchaseOrderId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_StatusIsNotApprovedOrReceiving_ThrowsPurchaseOrderCannotReceiveGoodsException()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);

        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, Guid.NewGuid())
        {
            ReceivedQuantity = 1
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderCannotReceiveGoodsException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_PurchaseOrderItemsIsNotFound_ThrowsPurchaseOrderItemIsNotFoundException()
    {
        var notFoundOrderItemId = Guid.NewGuid();
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, notFoundOrderItemId)
        {
            ReceivedQuantity = 1
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderItemIsNotFoundException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_ReceivedQuantityIsTooMuch_ThrowsPurchaseOrderReceiveQuantityExceedsOrderedQuantityException()
    {
        var orderedQuantity = 100;
        var receivedQuantity = 99;
        var wrongReceivingQuantity = 2;
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var product = new Product(Guid.NewGuid(), "product");
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, orderedQuantity, 0);
        purchaseOrderItem.AddQuantityReceived(receivedQuantity);
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            ReceivedQuantity = wrongReceivingQuantity
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderReceiveQuantityExceedsOrderedQuantityException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_ProductIsNotFound_ThrowsProductIsNotFoundException()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var product = new Product(Guid.NewGuid(), "product");
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0);
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        var dto = new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            ReceivedQuantity = 1
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var productDataReaderMock = ProductDataReader.NotFound(product.Id);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, productDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<ProductIsNotFoundException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_ProductTrackedAndWarehouseIdNotHaveValue_ThrowsArgumentException()
    {

        var purchaseOrder = new PurchaseOrder("code", null!, warehouseId: null, null!);
        var product = new Product(Guid.NewGuid(), "product");
        product.SetTrackInventory(true);
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
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, productDataReaderStub.Object, null!);

        await Assert.ThrowsAsync<ArgumentException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));
    }

    [Fact]
    public async Task ReceiveItemsAsync_ProductTrackedAndWarehouseIsNotFound_ThrowsWarehouseIsNotFoundException()
    {
        var notFoundWarehouseId = Guid.NewGuid();
        var purchaseOrder = new PurchaseOrder("code", null!, warehouseId: null, null!);
        var product = new Product(Guid.NewGuid(), "product");
        product.SetTrackInventory(true);
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
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, warehouseDataReaderMock.Object, null!, productDataReaderStub.Object, null!);

        await Assert.ThrowsAsync<WarehouseIsNotFoundException>(() => purchaseOrderManager.ReceiveItemsAsync(dto));

        warehouseDataReaderMock.Verify();
    }

    [Fact]
    public async Task ReceiveItemsAsync_UpdatePurchaseOrder()
    {
        var warehouseId = Guid.NewGuid();
        var purchaseOrder = new PurchaseOrder("code", null!, warehouseId, null!);
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
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryMock.Object, purchaseOrderDataReaderStub.Object, null!, null!, warehouseDataReaderMock.Object, null!, productDataReaderStub.Object, Mock.Of<IEventPublisher>());

        var receiveItemResult = await purchaseOrderManager.ReceiveItemsAsync(dto);

        Assert.Equal(dto.ReceivedQuantity, receiveItemResult.ReceivedQuantity);
        purchaseOrderRepositoryMock.Verify();
    }

    [Fact]
    public async Task ReceiveItemsAsync_ProductIsTrackedInventory_AddStockReveive()
    {
        var warehouseId = Guid.NewGuid();
        var purchaseOrder = new PurchaseOrder("code", null!, warehouseId, null!);
        var product = new Product(Guid.NewGuid(), "product");
        product.SetTrackInventory(true);
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
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryStub.Object, purchaseOrderDataReaderStub.Object, inventoryStockManagerMock.Object, null!, warehouseDataReaderMock.Object, null!, productDataReaderStub.Object, Mock.Of<IEventPublisher>());

        var receiveItemResult = await purchaseOrderManager.ReceiveItemsAsync(dto);

        Assert.Equal(dto.ReceivedQuantity, receiveItemResult.ReceivedQuantity);
        inventoryStockManagerMock.Verify();
    }

    #endregion

    #region VerifyStatusAsync

    [Fact]
    public async Task VerifyStatusAsync_PurchaseOrderIsNotFound_ThrowsPurchaseOrderIsNotFoundException()
    {
        var notFoundPurchaseOrderId = Guid.NewGuid();
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.NotFound(notFoundPurchaseOrderId);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<PurchaseOrderIsNotFoundException>(() => purchaseOrderManager.VerifyStatusAsync(notFoundPurchaseOrderId));

        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task VerifyStatusAsync_StatusIsCompleted_Returns()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Completed);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderRepositoryMock = PurchaseOrderRepository.UpdatePurchaseOrderWillReturns(purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!);

        await purchaseOrderManager.VerifyStatusAsync(purchaseOrder.Id);

        purchaseOrderRepositoryMock.Verify(repository => repository.UpdateAsync(It.IsAny<PurchaseOrder>()), Times.Never);
    }

    [Fact]
    public async Task VerifyStatusAsync_StatusIsCancelled_Returns()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, product.Id, 1, 0), productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Cancelled);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderRepositoryMock = PurchaseOrderRepository.UpdatePurchaseOrderWillReturns(purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!);

        await purchaseOrderManager.VerifyStatusAsync(purchaseOrder.Id);

        purchaseOrderRepositoryMock.Verify(repository => repository.UpdateAsync(It.IsAny<PurchaseOrder>()), Times.Never);
    }

    [Fact]
    public async Task VerifyStatusAsync_AllItemsReceived_ChangeStatusToCompletedAndUpdate()
    {
        var orderedQuantity = 100;
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
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
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryMock.Object, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!);

        await purchaseOrderManager.VerifyStatusAsync(purchaseOrder.Id);

        purchaseOrderRepositoryMock.Verify();
    }

    [Fact]
    public async Task VerifyStatusAsync_HasAnyItemsReceiveGoods_ChangeStatusToReceivingAndUpdate()
    {
        var receivedQuantity = 1;
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var product = new Product(Guid.NewGuid(), "product");
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        var purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, product.Id, 100, 0);
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, productDataReaderStub.Object);
        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted);
        purchaseOrderItem.AddQuantityReceived(receivedQuantity);

        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderRepositoryMock = new Mock<IRepository<PurchaseOrder>>();
        purchaseOrderRepositoryMock.Setup(repository => repository.UpdateAsync(It.Is<PurchaseOrder>(po => po.Status == PurchaseOrderStatus.Receiving)));
        var purchaseOrderManager = new PurchaseOrderManager(purchaseOrderRepositoryMock.Object, purchaseOrderDataReaderStub.Object, null!, null!, null!, null!, null!, null!);

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
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!);

        var nullResult = await purchaseOrderManager.GetPurchaseOrderByIdAsync(notFoundPurchaseOrderId);

        Assert.Null(nullResult);
        purchaseOrderDataReaderMock.Verify();
    }

    [Fact]
    public async Task GetPurchaseOrderByIdAsync_PurchaseOrderIsFound_ReturnsDto()
    {
        var purchaseOrder = new PurchaseOrder("code", null!, null!, null!);
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.PurchaseOrderById(purchaseOrder.Id, purchaseOrder);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!);

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
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => purchaseOrderManager.GetPurchaseOrdersAsync("", invalidPageIndex, 1));
    }

    [Fact]
    public async Task GetPurchaseOrdersAsync_PageSizeLessThanOrEqualTo0_ThrowsArgumentOutOfRangeException()
    {
        var invalidPageSize = 0;
        var purchaseOrderManager = new PurchaseOrderManager(null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => purchaseOrderManager.GetPurchaseOrdersAsync("", 0, invalidPageSize));
    }

    [Fact]
    public async Task GetPurchaseOrdersAsync_KeywordsIsEmpty_ReturnsOrderByCreatedDateDescendingOrders()
    {
        var pageIndex = 0;
        var pageSize = 1;
        var data = new[]
        {
            new PurchaseOrder("code-1", null!, null!, null!),
            new PurchaseOrder("code-2", null!, null!, null!),
            new PurchaseOrder("code-3", null!, null!, null!),
            new PurchaseOrder("code-4", null!, null!, null!), //last insert
        };
        var purchaseOrderDataReaderMock = PurchaseOrderDataReader.WithData(data);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderMock.Object, null!, null!, null!, null!, null!, null!);

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
            new PurchaseOrder("code-1", vendorHasKeywords.Id, null!, null!),
            new PurchaseOrder("code-2", null!, null!, null!),
            new PurchaseOrder("code-3", null!, warehouseHasKeywords.Id, null!),
            new PurchaseOrder("code-4", vendorHasKeywords.Id, warehouseHasKeywords.Id, null!),
        };
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.WithData(data);
        var vendorDataReaderMock = VendorDataReader.WithData(vendorHasKeywords);
        var warehouseDataReaderMock = WarehouseDataReader.WithData(warehouseHasKeywords);
        var purchaseOrderManager = new PurchaseOrderManager(null!, purchaseOrderDataReaderStub.Object, null!, vendorDataReaderMock.Object, warehouseDataReaderMock.Object, null!, null!, null!);

        var result = await purchaseOrderManager.GetPurchaseOrdersAsync(keywords, pageIndex, pageSize);

        Assert.Equal(3, result.PagerInfo.TotalCount);
        Assert.Equal(data[3].Id, result.First().Id);
        vendorDataReaderMock.Verify();
        warehouseDataReaderMock.Verify();
    }

    #endregion
}

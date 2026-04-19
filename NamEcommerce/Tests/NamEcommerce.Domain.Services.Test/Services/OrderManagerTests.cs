using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Services.Orders;
using NamEcommerce.Domain.Services.Test.Helpers;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Customers;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Exceptions.Users;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class OrderManagerTests
{
    private static Customer NewCustomer()
        => new(Guid.NewGuid(), "customer-name", "0900000000", "customer-address");

    private static Product NewProduct(Guid? id = null, string name = "product")
        => new(id ?? Guid.NewGuid(), name);

    private static User NewUser(Guid id)
        => new(id, "username", "fullName", "phoneNumber");

    private static CreateOrderDto BuildCreateOrderDto(Guid customerId, Guid? userId = null, params AddOrderItemDto[] items)
    {
        var dto = new CreateOrderDto
        {
            Code = "O-CODE-001",
            CustomerId = customerId,
            CreatedByUserId = userId,
            ShippingAddress = "shipping-address",
            ExpectedShippingDateUtc = DateTime.UtcNow.Date.AddDays(1),
            Note = "note",
            OrderDiscount = 0
        };
        foreach (var item in items)
            dto.Items.Add(item);
        return dto;
    }

    #region CreateOrderAsync

    [Fact]
    public async Task CreateOrderAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.CreateOrderAsync(null!));
    }

    [Fact]
    public async Task CreateOrderAsync_ExpectedShippingDateInPast_ThrowsOrderDataIsInvalidException()
    {
        var dto = new CreateOrderDto
        {
            Code = "O-001",
            CustomerId = Guid.NewGuid(),
            CreatedByUserId = null,
            ExpectedShippingDateUtc = DateTime.UtcNow.Date.AddDays(-1)
        };
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderDataIsInvalidException>(() => manager.CreateOrderAsync(dto));
    }

    [Fact]
    public async Task CreateOrderAsync_OrderDiscountNegative_ThrowsOrderDataIsInvalidException()
    {
        var dto = new CreateOrderDto
        {
            Code = "O-001",
            CustomerId = Guid.NewGuid(),
            CreatedByUserId = null,
            OrderDiscount = -1
        };
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderDataIsInvalidException>(() => manager.CreateOrderAsync(dto));
    }

    [Fact]
    public async Task CreateOrderAsync_ItemQuantityZero_ThrowsOrderItemDataIsInvalidException()
    {
        var dto = BuildCreateOrderDto(Guid.NewGuid(), null, new AddOrderItemDto
        {
            ProductId = Guid.NewGuid(),
            Quantity = 0,
            UnitPrice = 10
        });
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderItemDataIsInvalidException>(() => manager.CreateOrderAsync(dto));
    }

    [Fact]
    public async Task CreateOrderAsync_CodeAlreadyExists_ThrowsOrderCodeExistsException()
    {
        var customer = NewCustomer();
        var existingOrder = new Order("O-001");
        var orderDataReaderStub = OrderDataReader.HasOne(existingOrder);
        var customerDataReaderStub = CustomerDataReader.CustomerById(customer.Id, customer);
        var dto = new CreateOrderDto
        {
            Code = existingOrder.Code,
            CustomerId = customer.Id,
            CreatedByUserId = null
        };
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, customerDataReaderStub.Object, null!, null!);

        await Assert.ThrowsAsync<OrderCodeExistsException>(() => manager.CreateOrderAsync(dto));
    }

    [Fact]
    public async Task CreateOrderAsync_CustomerNotFound_ThrowsCustomerIsNotFoundException()
    {
        var notFoundCustomerId = Guid.NewGuid();
        var dto = BuildCreateOrderDto(notFoundCustomerId);
        var orderDataReaderStub = OrderDataReader.Empty();
        var customerDataReaderMock = CustomerDataReader.NotFound(notFoundCustomerId);
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, customerDataReaderMock.Object, null!, null!);

        await Assert.ThrowsAsync<CustomerIsNotFoundException>(() => manager.CreateOrderAsync(dto));
        customerDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreateOrderAsync_CreatedByUserNotFound_ThrowsUserIsNotFoundException()
    {
        var customer = NewCustomer();
        var notFoundUserId = Guid.NewGuid();
        var dto = BuildCreateOrderDto(customer.Id, notFoundUserId);
        var orderDataReaderStub = OrderDataReader.Empty();
        var customerDataReaderStub = CustomerDataReader.CustomerById(customer.Id, customer);
        var userDataReaderMock = UserDataReader.NotFound(notFoundUserId);
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, customerDataReaderStub.Object, userDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<UserIsNotFoundException>(() => manager.CreateOrderAsync(dto));
        userDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreateOrderAsync_ValidDto_CreatesOrderAndPublishesEvent()
    {
        var customer = NewCustomer();
        var product = NewProduct();
        var item = new AddOrderItemDto { ProductId = product.Id, Quantity = 2, UnitPrice = 100 };
        var dto = BuildCreateOrderDto(customer.Id, null, item);

        var orderDataReaderStub = OrderDataReader.Empty();
        var customerDataReaderStub = CustomerDataReader.CustomerById(customer.Id, customer);
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);

        // Insert stub: return back the order passed in
        var repositoryMock = new Mock<IRepository<Order>>();
        repositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order o) => o)
            .Verifiable();

        var manager = new OrderManager(
            repositoryMock.Object,
            orderDataReaderStub.Object,
            productDataReaderStub.Object,
            customerDataReaderStub.Object,
            null!,
            Mock.Of<IEventPublisher>());

        var result = await manager.CreateOrderAsync(dto);

        Assert.NotEqual(Guid.Empty, result.CreatedId);
        repositoryMock.Verify(r => r.InsertAsync(It.Is<Order>(o =>
            o.Code == dto.Code
            && o.CustomerId == customer.Id
            && o.OrderItems.Count() == 1
            && o.OrderSubTotal == 200
            && o.OrderTotal == 200)), Times.Once);
    }

    #endregion

    #region UpdateOrderAsync

    [Fact]
    public async Task UpdateOrderAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.UpdateOrderAsync(null!));
    }

    [Fact]
    public async Task UpdateOrderAsync_OrderNotFound_ThrowsOrderIsNotFoundException()
    {
        var notFoundId = Guid.NewGuid();
        var dto = new UpdateOrderDto(notFoundId);
        var orderDataReaderMock = OrderDataReader.NotFound(notFoundId);
        var manager = new OrderManager(null!, orderDataReaderMock.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderIsNotFoundException>(() => manager.UpdateOrderAsync(dto));
        orderDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateOrderAsync_OrderLocked_ThrowsOrderCannotUpdateInfoException()
    {
        var order = new Order("O-001");
        order.LockOrder("done");
        var dto = new UpdateOrderDto(order.Id) { Note = "new note" };
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderCannotUpdateInfoException>(() => manager.UpdateOrderAsync(dto));
    }

    [Fact]
    public async Task UpdateOrderAsync_ValidDto_UpdatesOrder()
    {
        var order = new Order("O-001");
        var dto = new UpdateOrderDto(order.Id)
        {
            Note = "new-note",
            OrderDiscount = 0,
            ExpectedShippingDateUtc = DateTime.UtcNow.Date.AddDays(2)
        };
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var repositoryMock = OrderRepository.UpdateAnyOrderWillReturns(order);
        var manager = new OrderManager(repositoryMock.Object, orderDataReaderStub.Object, null!, null!, null!, Mock.Of<IEventPublisher>());

        var result = await manager.UpdateOrderAsync(dto);

        Assert.Equal(order.Id, result.UpdatedId);
        Assert.Equal("new-note", order.Note);
        Assert.NotNull(order.UpdatedOnUtc);
        repositoryMock.Verify();
    }

    #endregion

    #region AddOrderItemAsync

    [Fact]
    public async Task AddOrderItemAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.AddOrderItemAsync(Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task AddOrderItemAsync_DtoInvalid_ThrowsOrderItemDataIsInvalidException()
    {
        var dto = new AddOrderItemDto { ProductId = Guid.NewGuid(), Quantity = 0, UnitPrice = 10 };
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderItemDataIsInvalidException>(() => manager.AddOrderItemAsync(Guid.NewGuid(), dto));
    }

    [Fact]
    public async Task AddOrderItemAsync_OrderNotFound_ThrowsOrderIsNotFoundException()
    {
        var notFoundOrderId = Guid.NewGuid();
        var dto = new AddOrderItemDto { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 1 };
        var orderDataReaderMock = OrderDataReader.NotFound(notFoundOrderId);
        var manager = new OrderManager(null!, orderDataReaderMock.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderIsNotFoundException>(() => manager.AddOrderItemAsync(notFoundOrderId, dto));
        orderDataReaderMock.Verify();
    }

    [Fact]
    public async Task AddOrderItemAsync_ProductNotFound_ThrowsProductIsNotFoundException()
    {
        var order = new Order("O-001");
        var dto = new AddOrderItemDto { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 10 };
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var productDataReaderMock = ProductDataReader.NotFound(dto.ProductId);
        var manager = new OrderManager(null!, orderDataReaderStub.Object, productDataReaderMock.Object, null!, null!, null!);

        await Assert.ThrowsAsync<ProductIsNotFoundException>(() => manager.AddOrderItemAsync(order.Id, dto));
        productDataReaderMock.Verify();
    }

    [Fact]
    public async Task AddOrderItemAsync_ValidDto_AddsOrderItemAndPublishesEvent()
    {
        var order = new Order("O-001");
        var product = NewProduct();
        var dto = new AddOrderItemDto { ProductId = product.Id, Quantity = 3, UnitPrice = 50 };
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        var repositoryMock = OrderRepository.UpdateAnyOrderWillReturns(order);
        var manager = new OrderManager(repositoryMock.Object, orderDataReaderStub.Object, productDataReaderStub.Object, null!, null!, Mock.Of<IEventPublisher>());

        await manager.AddOrderItemAsync(order.Id, dto);

        Assert.Single(order.OrderItems);
        Assert.Equal(150, order.OrderSubTotal);
        repositoryMock.Verify();
    }

    #endregion

    #region UpdateOrderItemAsync

    [Fact]
    public async Task UpdateOrderItemAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.UpdateOrderItemAsync(null!));
    }

    [Fact]
    public async Task UpdateOrderItemAsync_DtoInvalid_ThrowsOrderItemDataIsInvalidException()
    {
        var dto = new UpdateOrderItemDto
        {
            OrderId = Guid.NewGuid(),
            OrderItemId = Guid.NewGuid(),
            Quantity = 0,
            UnitPrice = 10
        };
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderItemDataIsInvalidException>(() => manager.UpdateOrderItemAsync(dto));
    }

    [Fact]
    public async Task UpdateOrderItemAsync_OrderNotFound_ThrowsOrderIsNotFoundException()
    {
        var notFoundOrderId = Guid.NewGuid();
        var dto = new UpdateOrderItemDto
        {
            OrderId = notFoundOrderId,
            OrderItemId = Guid.NewGuid(),
            Quantity = 1,
            UnitPrice = 1
        };
        var orderDataReaderMock = OrderDataReader.NotFound(notFoundOrderId);
        var manager = new OrderManager(null!, orderDataReaderMock.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderIsNotFoundException>(() => manager.UpdateOrderItemAsync(dto));
        orderDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateOrderItemAsync_OrderItemNotFound_ThrowsOrderItemIsNotFoundException()
    {
        var order = new Order("O-001");
        var dto = new UpdateOrderItemDto
        {
            OrderId = order.Id,
            OrderItemId = Guid.NewGuid(),
            Quantity = 1,
            UnitPrice = 1
        };
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderItemIsNotFoundException>(() => manager.UpdateOrderItemAsync(dto));
    }

    [Fact]
    public async Task UpdateOrderItemAsync_ValidDto_UpdatesItem()
    {
        var order = new Order("O-001");
        var product = NewProduct();
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await order.AddOrderItemAsync(product.Id, 100, 2, productDataReaderStub.Object);
        var addedItem = order.OrderItems.First();

        var dto = new UpdateOrderItemDto
        {
            OrderId = order.Id,
            OrderItemId = addedItem.Id,
            Quantity = 5,
            UnitPrice = 200
        };
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var repositoryMock = OrderRepository.UpdateAnyOrderWillReturns(order);
        var manager = new OrderManager(repositoryMock.Object, orderDataReaderStub.Object, null!, null!, null!, Mock.Of<IEventPublisher>());

        await manager.UpdateOrderItemAsync(dto);

        Assert.Equal(5, addedItem.Quantity);
        Assert.Equal(200, addedItem.UnitPrice);
        Assert.Equal(1000, order.OrderSubTotal);
        repositoryMock.Verify();
    }

    #endregion

    #region DeleteOrderItemAsync

    [Fact]
    public async Task DeleteOrderItemAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.DeleteOrderItemAsync(null!));
    }

    [Fact]
    public async Task DeleteOrderItemAsync_OrderNotFound_ThrowsOrderIsNotFoundException()
    {
        var notFoundOrderId = Guid.NewGuid();
        var dto = new DeleteOrderItemDto(notFoundOrderId, Guid.NewGuid());
        var orderDataReaderMock = OrderDataReader.NotFound(notFoundOrderId);
        var manager = new OrderManager(null!, orderDataReaderMock.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderIsNotFoundException>(() => manager.DeleteOrderItemAsync(dto));
        orderDataReaderMock.Verify();
    }

    [Fact]
    public async Task DeleteOrderItemAsync_OrderLocked_ThrowsOrderCannotUpdateOrderItemsException()
    {
        var order = new Order("O-001");
        order.LockOrder("done");
        var dto = new DeleteOrderItemDto(order.Id, Guid.NewGuid());
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderCannotUpdateOrderItemsException>(() => manager.DeleteOrderItemAsync(dto));
    }

    [Fact]
    public async Task DeleteOrderItemAsync_ValidDto_RemovesItem()
    {
        var order = new Order("O-001");
        var product = NewProduct();
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await order.AddOrderItemAsync(product.Id, 100, 2, productDataReaderStub.Object);
        var addedItem = order.OrderItems.First();

        var dto = new DeleteOrderItemDto(order.Id, addedItem.Id);
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var repositoryMock = OrderRepository.UpdateAnyOrderWillReturns(order);
        var manager = new OrderManager(repositoryMock.Object, orderDataReaderStub.Object, null!, null!, null!, Mock.Of<IEventPublisher>());

        await manager.DeleteOrderItemAsync(dto);

        Assert.Empty(order.OrderItems);
        Assert.Equal(0, order.OrderSubTotal);
        repositoryMock.Verify();
    }

    #endregion

    #region LockOrderAsync

    [Fact]
    public async Task LockOrderAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.LockOrderAsync(null!));
    }

    [Fact]
    public async Task LockOrderAsync_OrderNotFound_ThrowsOrderIsNotFoundException()
    {
        var notFoundOrderId = Guid.NewGuid();
        var dto = new LockOrderDto { OrderId = notFoundOrderId, Reason = "reason" };
        var orderDataReaderMock = OrderDataReader.NotFound(notFoundOrderId);
        var manager = new OrderManager(null!, orderDataReaderMock.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderIsNotFoundException>(() => manager.LockOrderAsync(dto));
        orderDataReaderMock.Verify();
    }

    [Fact]
    public async Task LockOrderAsync_AlreadyLocked_ThrowsOrderLockedException()
    {
        var order = new Order("O-001");
        order.LockOrder("first");
        var dto = new LockOrderDto { OrderId = order.Id, Reason = "second" };
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderLockedException>(() => manager.LockOrderAsync(dto));
    }

    [Fact]
    public async Task LockOrderAsync_ValidDto_LocksOrder()
    {
        var order = new Order("O-001");
        var dto = new LockOrderDto { OrderId = order.Id, Reason = "reason" };
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var repositoryMock = OrderRepository.UpdateAnyOrderWillReturns(order);
        var manager = new OrderManager(repositoryMock.Object, orderDataReaderStub.Object, null!, null!, null!, Mock.Of<IEventPublisher>());

        await manager.LockOrderAsync(dto);

        Assert.Equal(OrderStatus.Locked, order.OrderStatus);
        Assert.Equal("reason", order.LockOrderReason);
        repositoryMock.Verify();
    }

    #endregion

    #region UpdateShippingAsync

    [Fact]
    public async Task UpdateShippingAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.UpdateShippingAsync(null!));
    }

    [Fact]
    public async Task UpdateShippingAsync_DateInPast_ThrowsOrderDataIsInvalidException()
    {
        var dto = new UpdateShippingDto
        {
            OrderId = Guid.NewGuid(),
            ExpectedShippingDateUtc = DateTime.UtcNow.Date.AddDays(-1),
            Address = "addr"
        };
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderDataIsInvalidException>(() => manager.UpdateShippingAsync(dto));
    }

    [Fact]
    public async Task UpdateShippingAsync_OrderNotFound_ThrowsOrderIsNotFoundException()
    {
        var notFoundOrderId = Guid.NewGuid();
        var dto = new UpdateShippingDto
        {
            OrderId = notFoundOrderId,
            ExpectedShippingDateUtc = DateTime.UtcNow.Date.AddDays(1),
            Address = "addr"
        };
        var orderDataReaderMock = OrderDataReader.NotFound(notFoundOrderId);
        var manager = new OrderManager(null!, orderDataReaderMock.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderIsNotFoundException>(() => manager.UpdateShippingAsync(dto));
        orderDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateShippingAsync_ValidDto_UpdatesShipping()
    {
        var order = new Order("O-001");
        var newDate = DateTime.UtcNow.Date.AddDays(3);
        var dto = new UpdateShippingDto
        {
            OrderId = order.Id,
            ExpectedShippingDateUtc = newDate,
            Address = "new-address"
        };
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var repositoryMock = OrderRepository.UpdateAnyOrderWillReturns(order);
        var manager = new OrderManager(repositoryMock.Object, orderDataReaderStub.Object, null!, null!, null!, Mock.Of<IEventPublisher>());

        await manager.UpdateShippingAsync(dto);

        Assert.Equal(newDate, order.ExpectedShippingDateUtc);
        Assert.Equal("new-address", order.ShippingAddress);
        repositoryMock.Verify();
    }

    #endregion

    #region MarkOrderItemDeliveredAsync

    [Fact]
    public async Task MarkOrderItemDeliveredAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => manager.MarkOrderItemDeliveredAsync(null!));
    }

    [Fact]
    public async Task MarkOrderItemDeliveredAsync_OrderNotFound_ThrowsOrderIsNotFoundException()
    {
        var notFoundOrderId = Guid.NewGuid();
        var dto = new MarkOrderItemDeliveredDto
        {
            OrderId = notFoundOrderId,
            OrderItemId = Guid.NewGuid(),
            PictureId = Guid.NewGuid()
        };
        var orderDataReaderMock = OrderDataReader.NotFound(notFoundOrderId);
        var manager = new OrderManager(null!, orderDataReaderMock.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderIsNotFoundException>(() => manager.MarkOrderItemDeliveredAsync(dto));
        orderDataReaderMock.Verify();
    }

    [Fact]
    public async Task MarkOrderItemDeliveredAsync_ItemNotFound_ThrowsOrderItemIsNotFoundException()
    {
        var order = new Order("O-001");
        var dto = new MarkOrderItemDeliveredDto
        {
            OrderId = order.Id,
            OrderItemId = Guid.NewGuid(),
            PictureId = Guid.NewGuid()
        };
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<OrderItemIsNotFoundException>(() => manager.MarkOrderItemDeliveredAsync(dto));
    }

    [Fact]
    public async Task MarkOrderItemDeliveredAsync_ValidDto_MarksDelivered()
    {
        var order = new Order("O-001");
        var productA = NewProduct(Guid.NewGuid(), "A");
        var productB = NewProduct(Guid.NewGuid(), "B");
        var productDataReaderStub = ProductDataReader
            .ProductById(productA.Id, productA)
            .ProductById(productB.Id, productB);
        await order.AddOrderItemAsync(productA.Id, 10, 1, productDataReaderStub.Object);
        await order.AddOrderItemAsync(productB.Id, 10, 1, productDataReaderStub.Object);
        var firstItem = order.OrderItems.First();

        var dto = new MarkOrderItemDeliveredDto
        {
            OrderId = order.Id,
            OrderItemId = firstItem.Id,
            PictureId = Guid.NewGuid()
        };
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var repositoryMock = OrderRepository.UpdateAnyOrderWillReturns(order);
        var manager = new OrderManager(repositoryMock.Object, orderDataReaderStub.Object, null!, null!, null!, Mock.Of<IEventPublisher>());

        await manager.MarkOrderItemDeliveredAsync(dto);

        Assert.True(firstItem.IsDelivered);
        Assert.NotEqual(OrderStatus.Locked, order.OrderStatus); // only 1 of 2 delivered
        repositoryMock.Verify();
    }

    [Fact]
    public async Task MarkOrderItemDeliveredAsync_AllItemsDelivered_AutoLocksOrder()
    {
        var order = new Order("O-001");
        var product = NewProduct();
        var productDataReaderStub = ProductDataReader.ProductById(product.Id, product);
        await order.AddOrderItemAsync(product.Id, 10, 1, productDataReaderStub.Object);
        var onlyItem = order.OrderItems.First();

        var dto = new MarkOrderItemDeliveredDto
        {
            OrderId = order.Id,
            OrderItemId = onlyItem.Id,
            PictureId = Guid.NewGuid()
        };
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var repositoryMock = OrderRepository.UpdateAnyOrderWillReturns(order);
        var manager = new OrderManager(repositoryMock.Object, orderDataReaderStub.Object, null!, null!, null!, Mock.Of<IEventPublisher>());

        await manager.MarkOrderItemDeliveredAsync(dto);

        Assert.True(onlyItem.IsDelivered);
        Assert.Equal(OrderStatus.Locked, order.OrderStatus);
        Assert.NotNull(order.LockOrderReason);
    }

    #endregion

    #region GetOrderByIdAsync

    [Fact]
    public async Task GetOrderByIdAsync_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var orderDataReaderStub = OrderDataReader.NotFound(id);
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, null!, null!, null!);

        var result = await manager.GetOrderByIdAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrderByIdAsync_Found_ReturnsOrderDto()
    {
        var order = new Order("O-001");
        var orderDataReaderStub = OrderDataReader.OrderById(order.Id, order);
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, null!, null!, null!);

        var dto = await manager.GetOrderByIdAsync(order.Id);

        Assert.NotNull(dto);
        Assert.Equal(order.Id, dto!.Id);
        Assert.Equal(order.Code, dto.Code);
    }

    #endregion

    #region DoesCodeExistAsync

    [Fact]
    public async Task DoesCodeExistAsync_CodeIsEmpty_ThrowsArgumentException()
    {
        var manager = new OrderManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentException>(() => manager.DoesCodeExistAsync(string.Empty));
    }

    [Fact]
    public async Task DoesCodeExistAsync_CodeExists_ReturnsTrue()
    {
        var existing = new Order("O-EXISTS");
        var orderDataReaderStub = OrderDataReader.HasOne(existing);
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, null!, null!, null!);

        var exists = await manager.DoesCodeExistAsync("O-EXISTS");

        Assert.True(exists);
    }

    [Fact]
    public async Task DoesCodeExistAsync_CodeDoesNotExist_ReturnsFalse()
    {
        var orderDataReaderStub = OrderDataReader.Empty();
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, null!, null!, null!);

        var exists = await manager.DoesCodeExistAsync("O-NEW");

        Assert.False(exists);
    }

    [Fact]
    public async Task DoesCodeExistAsync_CodeExistsOnSameId_ReturnsFalse()
    {
        var existing = new Order("O-EXISTS");
        var orderDataReaderStub = OrderDataReader.HasOne(existing);
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, null!, null!, null!);

        var exists = await manager.DoesCodeExistAsync("O-EXISTS", existing.Id);

        Assert.False(exists);
    }

    #endregion

    #region GetOrdersAsync

    [Fact]
    public async Task GetOrdersAsync_NoFilters_ReturnsPagedData()
    {
        var order1 = new Order("O-001");
        var order2 = new Order("O-002");
        var orderDataReaderStub = OrderDataReader.WithData(order1, order2);
        var customerDataReaderStub = CustomerDataReader.Empty();
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, customerDataReaderStub.Object, null!, null!);

        var paged = await manager.GetOrdersAsync(keywords: null, status: (OrderStatus?)null, pageIndex: 0, pageSize: 10);

        Assert.Equal(2, paged.PagerInfo.TotalCount);
    }

    [Fact]
    public async Task GetOrdersAsync_FilterByStatus_ReturnsMatchingOrders()
    {
        var pendingOrder = new Order("O-PENDING");
        var lockedOrder = new Order("O-LOCKED");
        lockedOrder.LockOrder("done");
        var orderDataReaderStub = OrderDataReader.WithData(pendingOrder, lockedOrder);
        var customerDataReaderStub = CustomerDataReader.Empty();
        var manager = new OrderManager(null!, orderDataReaderStub.Object, null!, customerDataReaderStub.Object, null!, null!);

        var paged = await manager.GetOrdersAsync(keywords: null, status: OrderStatus.Locked, pageIndex: 0, pageSize: 10);

        Assert.Equal(1, paged.PagerInfo.TotalCount);
    }

    #endregion
}

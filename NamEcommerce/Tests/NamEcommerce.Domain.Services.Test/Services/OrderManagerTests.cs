using NamEcommerce.TestHelper;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using System.Linq.Expressions;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class OrderManagerTests
{
    #region AddOrderItemAsync

    [Fact]
    public async Task AddOrderItemAsync_OrderNotFound_ThrowsArgumentException()
    {
        var readerMock = EntityDataReader.Create<Order>().WhenCall(r => r.GetByIdAsync(It.IsAny<Guid>()), (Order?)null);
        var orderManager = new NamEcommerce.Domain.Services.Orders.OrderManager(null!, readerMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => orderManager.AddOrderItemAsync(new AddOrderItemDto(Guid.NewGuid(), Guid.NewGuid(), 1, 10)));
    }

    [Fact]
    public async Task AddOrderItemAsync_InvalidQuantity_ThrowsArgumentException()
    {
        var orderManager = new NamEcommerce.Domain.Services.Orders.OrderManager(null!, null!);

        await Assert.ThrowsAsync<ArgumentException>(() => orderManager.AddOrderItemAsync(new AddOrderItemDto(Guid.NewGuid(), Guid.NewGuid(), 0, 10)));
    }

    [Fact]
    public async Task AddOrderItemAsync_OrderFound_AddsItemAndUpdatesRepository()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), 0, PaymentStatus.Pending, OrderStatus.Pending, new List<OrderItem>());
        var repoMock = Repository.Create<Order>().WhenCall(r => r.UpdateAsync(It.IsAny<Order>()), order);
        var readerMock = EntityDataReader.Create<Order>().WhenCall(r => r.GetByIdAsync(order.Id), order);
        var orderManager = new NamEcommerce.Domain.Services.Orders.OrderManager(repoMock.Object, readerMock.Object);

        await orderManager.AddOrderItemAsync(new AddOrderItemDto(order.Id, Guid.NewGuid(), 2, 5));

        readerMock.Verify();
        repoMock.Verify();
    }

    #endregion

    #region ChangeOrderStatusAsync

    [Fact]
    public async Task ChangeOrderStatusAsync_OrderNotFound_ThrowsArgumentException()
    {
        var readerMock = EntityDataReader.Create<Order>().WhenCall(r => r.GetByIdAsync(It.IsAny<Guid>()), (Order?)null);
        var orderManager = new NamEcommerce.Domain.Services.Orders.OrderManager(null!, readerMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => orderManager.ChangeOrderStatusAsync(Guid.NewGuid(), 1));
    }

    [Fact]
    public async Task ChangeOrderStatusAsync_CannotChange_ThrowsInvalidOperationException()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), 0, PaymentStatus.Pending, OrderStatus.Completed, new List<OrderItem>());
        var readerMock = EntityDataReader.Create<Order>().WhenCall(r => r.GetByIdAsync(order.Id), order);
        var orderManager = new NamEcommerce.Domain.Services.Orders.OrderManager(null!, readerMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => orderManager.ChangeOrderStatusAsync(order.Id, (int)OrderStatus.Processing));
        readerMock.Verify();
    }

    [Fact]
    public async Task ChangeOrderStatusAsync_ValidChange_UpdatesRepository()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), 0, PaymentStatus.Pending, OrderStatus.Pending, new List<OrderItem>());
        var repoMock = Repository.Create<Order>().WhenCall(r => r.UpdateAsync(It.IsAny<Order>()), order);
        var readerMock = EntityDataReader.Create<Order>().WhenCall(r => r.GetByIdAsync(order.Id), order);
        var orderManager = new NamEcommerce.Domain.Services.Orders.OrderManager(repoMock.Object, readerMock.Object);

        await orderManager.ChangeOrderStatusAsync(order.Id, (int)OrderStatus.Processing);

        readerMock.Verify();
        repoMock.Verify();
    }

    #endregion
}

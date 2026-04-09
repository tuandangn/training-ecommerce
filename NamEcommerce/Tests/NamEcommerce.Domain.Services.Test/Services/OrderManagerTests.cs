using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Services.Orders;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class OrderManagerTests
{
    #region AddOrderItemAsync

    [Fact]
    public async Task AddOrderItemAsync_OrderNotFound_ThrowsArgumentException()
    {
        var readerMock = EntityDataReader.Create<Order>().WhenCall(r => r.GetByIdAsync(It.IsAny<Guid>()), (Order?)null);
        var orderManager = new OrderManager(null!, readerMock.Object, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentException>(() => orderManager.AddOrderItemAsync(Guid.NewGuid(), new AddOrderItemDto
        {
            ProductId = Guid.NewGuid(),
            Quantity = 1,
            UnitPrice = 10
        }));
    }

    [Fact]
    public async Task AddOrderItemAsync_InvalidQuantity_ThrowsArgumentException()
    {
        var orderManager = new OrderManager(null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentException>(() => orderManager.AddOrderItemAsync(Guid.NewGuid(), new AddOrderItemDto
        {
            ProductId = Guid.NewGuid(),
            Quantity = 0,
            UnitPrice = 10
        }));
    }

    [Fact]
    public async Task AddOrderItemAsync_OrderFound_AddsItemAndUpdatesRepository()
    {
        var order = new Order("code", Guid.NewGuid(), 0, null);
        var repoMock = Repository.Create<Order>().WhenCall(r => r.UpdateAsync(It.IsAny<Order>()), order);
        var readerMock = EntityDataReader.Create<Order>().WhenCall(r => r.GetByIdAsync(order.Id), order);
        var orderManager = new OrderManager(repoMock.Object, readerMock.Object, null!, null!, null!, null!, null!);

        await orderManager.AddOrderItemAsync(order.Id, new AddOrderItemDto
        {
            ProductId = Guid.NewGuid(),
            Quantity = 2,
            UnitPrice = 5
        });

        readerMock.Verify();
        repoMock.Verify();
    }

    #endregion

    #region ChangeOrderStatusAsync

    [Fact]
    public async Task ChangeOrderStatusAsync_OrderNotFound_ThrowsArgumentException()
    {
        var readerMock = EntityDataReader.Create<Order>().WhenCall(r => r.GetByIdAsync(It.IsAny<Guid>()), (Order?)null);
        var orderManager = new OrderManager(null!, readerMock.Object, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentException>(() => orderManager.ChangeOrderStatusAsync(Guid.NewGuid(), OrderStatus.Pending));
    }

    [Fact]
    public async Task ChangeOrderStatusAsync_CannotChange_ThrowsInvalidOperationException()
    {
        var order = new Order("code", Guid.NewGuid(), 0, null);
        var readerMock = EntityDataReader.Create<Order>().WhenCall(r => r.GetByIdAsync(order.Id), order);
        var orderManager = new OrderManager(null!, readerMock.Object, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<InvalidOperationException>(() => orderManager.ChangeOrderStatusAsync(order.Id, OrderStatus.Processing));
        readerMock.Verify();
    }

    [Fact]
    public async Task ChangeOrderStatusAsync_ValidChange_UpdatesRepository()
    {
        var order = new Order("code", Guid.NewGuid(), 0, null);
        var repoMock = Repository.Create<Order>().WhenCall(r => r.UpdateAsync(It.IsAny<Order>()), order);
        var readerMock = EntityDataReader.Create<Order>().WhenCall(r => r.GetByIdAsync(order.Id), order);
        var orderManager = new OrderManager(repoMock.Object, readerMock.Object, null!, null!, null!, null!, null!);

        await orderManager.ChangeOrderStatusAsync(order.Id, OrderStatus.Processing);

        readerMock.Verify();
        repoMock.Verify();
    }

    #endregion
}

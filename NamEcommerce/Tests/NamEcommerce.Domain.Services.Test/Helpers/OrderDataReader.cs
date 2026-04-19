using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class OrderDataReader
{
    public static Mock<IEntityDataReader<Order>> Empty()
        => EntityDataReader.Create<Order>().WithData(Array.Empty<Order>());

    public static Mock<IEntityDataReader<Order>> WithData(params Order[] orders)
        => EntityDataReader.Create<Order>().WithData(orders);

    public static Mock<IEntityDataReader<Order>> HasOne(Order order)
        => EntityDataReader.Create<Order>().WithData(order);

    public static Mock<IEntityDataReader<Order>> NotFound(Guid id)
        => EntityDataReader.Create<Order>().WhenCall(reader => reader.GetByIdAsync(id), (Order?)null);

    public static Mock<IEntityDataReader<Order>> OrderById(Guid id, Order order)
        => EntityDataReader.Create<Order>().WhenCall(reader => reader.GetByIdAsync(id), order);

    public static Mock<IEntityDataReader<Order>> OrderById(this Mock<IEntityDataReader<Order>> mock, Guid id, Order order)
        => mock.WhenCall(reader => reader.GetByIdAsync(id), order);
}

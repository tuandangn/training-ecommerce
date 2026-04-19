using NamEcommerce.Domain.Entities.Orders;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class OrderRepository
{
    public static Mock<IRepository<Order>> CreateOrderWillReturns(Order @return)
        => Repository.Create<Order>().WhenCall(r =>
            r.InsertAsync(It.Is<Order>(entity =>
                entity.Code == @return.Code
                && entity.CustomerId == @return.CustomerId
                && entity.CreatedByUserId == @return.CreatedByUserId
                && entity.Note == @return.Note
                && entity.ExpectedShippingDateUtc == @return.ExpectedShippingDateUtc
                && entity.ShippingAddress == @return.ShippingAddress
                && entity.OrderDiscount == @return.OrderDiscount
                && entity.OrderItems.Count() == @return.OrderItems.Count()
                && entity.OrderItems.All(item => @return.OrderItems.Any(rItem =>
                    rItem.ProductId == item.ProductId
                    && rItem.Quantity == item.Quantity
                    && rItem.UnitPrice == item.UnitPrice))
            ))
        , @return);

    public static Mock<IRepository<Order>> UpdateOrderWillReturns(Order @return)
        => Repository.Create<Order>().WhenCall(r =>
            r.UpdateAsync(It.Is<Order>(entity =>
                entity.Id == @return.Id
                && entity.Code == @return.Code
                && entity.OrderStatus == @return.OrderStatus
                && entity.OrderDiscount == @return.OrderDiscount
                && entity.Note == @return.Note
                && entity.ExpectedShippingDateUtc == @return.ExpectedShippingDateUtc
                && entity.ShippingAddress == @return.ShippingAddress
                && entity.OrderItems.Count() == @return.OrderItems.Count()
            ))
        , @return);

    public static Mock<IRepository<Order>> UpdateAnyOrderWillReturns(Order @return)
        => Repository.Create<Order>().WhenCall(r => r.UpdateAsync(It.IsAny<Order>()), @return);
}

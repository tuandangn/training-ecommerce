using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Customers;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Orders;

[Serializable]
public sealed record Order : AppAggregateEntity
{
    internal Order(string code) : base(Guid.NewGuid())
    {
        Code = code;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; }
    public Guid CustomerId { get; private set; }
    public Guid? CreatedByUserId { get; init; }

    public decimal OrderTotal { get; private set; }
    public decimal OrderDiscount { get; private set; }
    public OrderStatus OrderStatus { get; private set; }
    public string? LockOrderReason { get; private set; }
    public string? Note { get; internal set; }

    public DateTime? ExpectedShippingDateUtc { get; set; }
    public string? ShippingAddress
    {
        get;
        internal set
        {
            field = value;
            NormalizedShippingAddress = TextHelper.Normalize(ShippingAddress);
        }
    }
    internal string NormalizedShippingAddress { get; private set; } = "";

    private readonly List<OrderItem> _orderItems = [];
    public IEnumerable<OrderItem> OrderItems => _orderItems.AsReadOnly();

    public DateTime CreatedOnUtc { get; }
    public DateTime? UpdatedOnUtc { get; internal set; }

    #region Methods

    internal async Task SetCustomerAsync(Guid customerId, IGetByIdService<Customer> byIdGetter)
    {
        ArgumentNullException.ThrowIfNull(byIdGetter);

        var customer = await byIdGetter.GetByIdAsync(customerId).ConfigureAwait(false);
        if (customer is null)
            throw new CustomerIsNotFoundException(customerId);

        CustomerId = customerId;
        if (string.IsNullOrEmpty(ShippingAddress))
            ShippingAddress = customer.Address;
    }

    internal async Task AddOrderItemAsync(Guid productId, decimal unitPrice, decimal quantity, IGetByIdService<Product> byIdGetter)
    {
        ArgumentNullException.ThrowIfNull(byIdGetter);

        if (!CanUpdateOrderItems())
            throw new OrderCannotUpdateOrderItemsException();

        var product = await byIdGetter.GetByIdAsync(productId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        var orderItem = new OrderItem(Id, productId, unitPrice, quantity)
        {
            //*TODO*
            CostPrice = product.CostPrice
        };
        _orderItems.Add(orderItem);

        RecalculateTotal();
    }

    internal void UpdateOrderItem(Guid orderItemId, decimal quantity, decimal unitPrice)
    {
        if (!CanUpdateOrderItems())
            throw new OrderCannotUpdateOrderItemsException();

        var orderItem = _orderItems.FirstOrDefault(item => item.Id == orderItemId);
        if (orderItem is null)
            throw new OrderItemIsNotFoundException(orderItemId);

        orderItem.Update(quantity, unitPrice);

        RecalculateTotal();
    }

    internal void RemoveOrderItem(Guid itemId)
    {
        if (!CanUpdateOrderItems())
            throw new OrderCannotUpdateOrderItemsException();

        var item = _orderItems.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return;

        _orderItems.Remove(item);

        RecalculateTotal();
    }

    internal void SetOrderDiscount(decimal? orderDiscount)
    {
        if(!orderDiscount.HasValue)
        {
            OrderDiscount = 0;
            RecalculateTotal();
            return;
        }

        if (orderDiscount.Value < 0)
            throw new OrderDiscountIsInvalidException("Order discount must greater than or equal to 0.");

        if (orderDiscount.Value > OrderTotal)
            throw new OrderDiscountIsInvalidException("Order discount cannot exceed order total.");

        OrderDiscount = orderDiscount.Value;
        RecalculateTotal();
    }

    private void RecalculateTotal()
    {
        OrderTotal = _orderItems.Sum(i => i.Price) - OrderDiscount;
    }

    internal bool CanUpdateInfo() => OrderStatus != OrderStatus.Locked;
    internal bool CanChangeStatusTo(OrderStatus toStatus)
    {
        if (!CanUpdateInfo())
            return false;

        return Enum.IsDefined(toStatus);
    }
    internal bool CanUpdateOrderItems()
    {
        if (!CanUpdateInfo())
            return false;

        return true;
    }
    internal bool CanLockOrder()
    {
        if (!CanUpdateInfo())
            return false;

        return true;
    }

    internal void ChangeStatus(OrderStatus status)
    {
        if (!CanChangeStatusTo(status))
            throw new OrderCannotChangeStatusException();

        OrderStatus = status;
    }

    internal void LockOrder(string? reason)
    {
        if (OrderStatus == OrderStatus.Locked)
            throw new OrderLockedException();

        ChangeStatus(OrderStatus.Locked);
        LockOrderReason = reason;
    }

    #endregion
}

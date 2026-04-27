using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Customers;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Orders;

[Serializable]
public sealed record Order : AppAggregateEntity
{
    public const string OrderCodePrefix = "DB";

    internal Order(string code) : base(Guid.NewGuid())
    {
        Code = code;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; }
    public DateTime? ExpectedShippingDateUtc { get; internal set; }
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
    public decimal OrderSubTotal { get; private set; }
    public decimal OrderTotal { get; private set; }
    public decimal OrderDiscount { get; private set; }
    public OrderStatus OrderStatus { get; private set; }
    public string? LockOrderReason { get; private set; }
    public string? Note { get; internal set; }

    public Guid CustomerId { get; private set; }
    internal string? CustomerName { get; private set; }
    internal string? CustomerPhone { get; private set; }
    internal string? CustomerAddress { get; private set; }

    public Guid? CreatedByUserId { get; init; }
    internal string? CreatedByUsername { get; set; }

    public DateTime CreatedOnUtc { get; }
    public DateTime? UpdatedOnUtc { get; internal set; }

    #region Events

    /// <summary>
    /// Đánh dấu đơn vừa được khởi tạo xong (sau khi setup customer + items + discount).
    /// Manager gọi method này NGAY TRƯỚC khi insert vào repository để raise <see cref="OrderPlaced"/>.
    /// </summary>
    internal void Place() => RaiseDomainEvent(new OrderPlaced(Id, Code, CustomerId, OrderTotal));

    /// <summary>
    /// Đánh dấu thông tin chung (note, expected shipping date, discount...) vừa được cập nhật.
    /// Manager gọi method này sau khi set properties để raise <see cref="OrderInfoUpdated"/>.
    /// </summary>
    internal void MarkInfoUpdated() => RaiseDomainEvent(new OrderInfoUpdated(Id));

    /// <summary>
    /// Đánh dấu thông tin shipping (address / expected date) vừa được cập nhật để raise <see cref="OrderShippingUpdated"/>.
    /// </summary>
    internal void MarkShippingUpdated() => RaiseDomainEvent(new OrderShippingUpdated(Id));

    /// <summary>
    /// Đánh dấu đơn đang bị xoá (soft delete) — raise <see cref="OrderDeleted"/>.
    /// Manager gọi TRƯỚC khi <c>repository.DeleteAsync</c>.
    /// </summary>
    internal void MarkDeleted() => RaiseDomainEvent(new OrderDeleted(Id, Code));

    #endregion

    #region Methods

    internal async Task SetCustomerAsync(Guid customerId, IGetByIdService<Customer> byIdGetter)
    {
        ArgumentNullException.ThrowIfNull(byIdGetter);

        var customer = await byIdGetter.GetByIdAsync(customerId).ConfigureAwait(false);
        if (customer is null)
            throw new CustomerIsNotFoundException(customerId);

        CustomerId = customerId;
        CustomerName = customer.FullName;
        CustomerPhone = customer.PhoneNumber;
        CustomerAddress = customer.Address;
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
            CostPrice = product.CostPrice,
            ProductName = product.Name
        };
        _orderItems.Add(orderItem);

        RecalculateTotal();

        RaiseDomainEvent(new OrderItemAdded(Id, orderItem.Id, productId, quantity, unitPrice));
    }

    internal void UpdateOrderItem(Guid orderItemId, decimal quantity, decimal unitPrice)
    {
        if (!CanUpdateOrderItems())
            throw new OrderCannotUpdateOrderItemsException();

        var orderItem = _orderItems.FirstOrDefault(item => item.Id == orderItemId);
        if (orderItem is null)
            throw new OrderItemIsNotFoundException(orderItemId);

        var calculatedSubTotal = OrderSubTotal - orderItem.SubTotal + quantity * unitPrice;
        if (OrderDiscount > calculatedSubTotal)
            throw new OrderDiscountIsInvalidException("Chiết khấu không được vượt quá tổng tiền hàng");

        orderItem.Update(quantity, unitPrice);

        RecalculateTotal();

        RaiseDomainEvent(new OrderItemUpdated(Id, orderItemId, quantity, unitPrice));
    }

    internal void RemoveOrderItem(Guid itemId)
    {
        if (!CanUpdateOrderItems())
            throw new OrderCannotUpdateOrderItemsException();

        var orderItem = _orderItems.FirstOrDefault(i => i.Id == itemId);
        if (orderItem is null)
            return;

        var calculatedSubTotal = OrderSubTotal - orderItem.SubTotal;
        if (OrderDiscount > calculatedSubTotal)
            throw new OrderDiscountIsInvalidException("Chiết khấu không được vượt quá tổng tiền hàng");

        _orderItems.Remove(orderItem);

        RecalculateTotal();

        RaiseDomainEvent(new OrderItemRemoved(Id, itemId));
    }

    internal void SetOrderDiscount(decimal? orderDiscount)
    {
        if (!orderDiscount.HasValue)
        {
            OrderDiscount = 0;
            RecalculateTotal();
            return;
        }

        if (orderDiscount.Value < 0)
            throw new OrderDiscountIsInvalidException("Order discount must greater than or equal to 0.");

        if (orderDiscount.Value > OrderSubTotal)
            throw new OrderDiscountIsInvalidException("Chiết khấu không được vượt quá tổng tiền hàng");

        OrderDiscount = orderDiscount.Value;
        RecalculateTotal();
    }

    private void RecalculateTotal()
    {
        OrderSubTotal = _orderItems.Sum(i => i.SubTotal);
        OrderTotal = OrderSubTotal - OrderDiscount;
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

        RaiseDomainEvent(new OrderLocked(Id, reason));
    }

    internal void MarkOrderItemDelivered(Guid orderItemId, Guid pictureId)
    {
        var orderItem = _orderItems.FirstOrDefault(i => i.Id == orderItemId);
        if (orderItem is null)
            throw new OrderItemIsNotFoundException(orderItemId);

        orderItem.MarkDelivered(pictureId);

        RaiseDomainEvent(new OrderItemDelivered(Id, orderItemId, pictureId));
    }
    internal bool TryAutoLock()
    {
        if (OrderStatus == OrderStatus.Locked)
            return false;

        if (!AreAllItemsDelivered())
            return false;

        LockOrder("Tất cả hàng hóa đã được giao.");
        return true;
    }
    private bool AreAllItemsDelivered() => _orderItems.Count > 0 && _orderItems.All(i => i.IsDelivered);

    #endregion
}

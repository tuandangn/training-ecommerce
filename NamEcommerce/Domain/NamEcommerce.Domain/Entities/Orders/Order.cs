using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Events.Orders;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Customers;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Values;
using NamEcommerce.Domain.Metadata;

namespace NamEcommerce.Domain.Entities.Orders;

[Serializable]
public sealed record Order : AppAggregateEntity
{
    public const string CODE_PREFIX = "DB";

    internal Order(string code) : this(code, null)
    {
    }

    internal Order(string code, CurrentUserInfoDto? createdByUser) : base(Guid.NewGuid())
    {
        Code = code;
        CreatedByUserId = createdByUser?.Id;
        CreatedByUsername = createdByUser?.Username;
        CustomerInfo = new CustomerInfo(string.Empty, string.Empty, string.Empty);
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; }
    public DateTime? ExpectedShippingDateUtc { get; internal set; }
    public NormalizableString ShippingAddress { get; internal set;  }
    public decimal OrderSubTotal { get; private set; }
    public decimal OrderTotal { get; private set; }
    public decimal OrderDiscount { get; private set; }
    public OrderStatus OrderStatus { get; private set; }
    public DateTime? CompletedOnUtc { get; private set; }
    public string? Note { get; internal set; }

    public Guid CustomerId { get; private set; }
    internal CustomerInfo CustomerInfo { get; private set; }

    private readonly List<OrderItem> _orderItems = [];
    public IEnumerable<OrderItem> OrderItems => _orderItems.AsReadOnly();

    public Guid? CreatedByUserId { get; private set; }
    internal string? CreatedByUsername { get; set; }

    public DateTime CreatedOnUtc { get; }
    public DateTime? UpdatedOnUtc { get; internal set; }

    #region Events

    internal void Place() => RaiseDomainEvent(new OrderPlaced(Id, Code, CustomerId, OrderTotal));

    internal void MarkInfoUpdated() => RaiseDomainEvent(new OrderInfoUpdated(Id));

    internal void MarkShippingUpdated() => RaiseDomainEvent(new OrderShippingUpdated(Id));

    internal void MarkDeleted()
    {
        // Cancelled orders already released reservation when the order was cancelled.
        IReadOnlyCollection<OrderReservationItem> reservationItems = OrderStatus == OrderStatus.Cancelled
            ? []
            : GetReservationItems();

        RaiseDomainEvent(new OrderDeleted(Id, Code, reservationItems));
    }

    internal void RaiseSoCancelledWithDirectShipReceived(IReadOnlyList<Guid> allocationIds)
        => RaiseDomainEvent(new SoCancelledWithDirectShipReceived(Id, allocationIds));

    #endregion

    #region Methods

    internal async Task SetCustomerAsync(Guid customerId, IGetByIdService<Customer> byIdGetter)
    {
        ArgumentNullException.ThrowIfNull(byIdGetter);

        var customer = await byIdGetter.GetByIdAsync(customerId).ConfigureAwait(false);
        if (customer is null)
            throw new CustomerIsNotFoundException(customerId);

        CustomerId = customerId;
        CustomerInfo = new CustomerInfo(customer.FullName, customer.PhoneNumber, customer.Address);
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

        var oldQuantity = orderItem.Quantity;
        orderItem.Update(quantity, unitPrice);

        RecalculateTotal();

        RaiseDomainEvent(new OrderItemUpdated(Id, orderItemId, orderItem.ProductId, oldQuantity, quantity, unitPrice));
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

        var productId = orderItem.ProductId;
        var quantity = orderItem.Quantity;

        _orderItems.Remove(orderItem);

        RecalculateTotal();

        RaiseDomainEvent(new OrderItemRemoved(Id, itemId, productId, quantity));
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

    internal bool CanUpdateInfo() => OrderStatus != OrderStatus.Completed && OrderStatus != OrderStatus.Cancelled;
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
    internal bool CanCompleteOrder() => OrderStatus == OrderStatus.Pending;

    internal void ChangeStatus(OrderStatus status)
    {
        if (!CanChangeStatusTo(status))
            throw new OrderCannotChangeStatusException();

        OrderStatus = status;
    }

    internal void Complete()
    {
        if (!CanCompleteOrder())
            throw new OrderCannotChangeStatusException();

        ChangeStatus(OrderStatus.Completed);
        CompletedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new OrderCompleted(Id));
    }

    internal void Cancel()
    {
        if (!CanUpdateInfo())
            throw new OrderCannotChangeStatusException();

        ChangeStatus(OrderStatus.Cancelled);
        RaiseDomainEvent(new OrderCancelled(Id, GetReservationItems()));
    }

    internal void MarkOrderItemDelivered(Guid orderItemId, Guid pictureId)
    {
        var orderItem = _orderItems.FirstOrDefault(i => i.Id == orderItemId);
        if (orderItem is null)
            throw new OrderItemIsNotFoundException(orderItemId);

        orderItem.MarkDelivered(pictureId);

        RaiseDomainEvent(new OrderItemDelivered(Id, orderItemId, pictureId));
    }

    internal void MarkOrderItemReceivedByCustomer(Guid orderItemId)
    {
        var orderItem = _orderItems.FirstOrDefault(i => i.Id == orderItemId);
        if (orderItem is null)
            throw new OrderItemIsNotFoundException(orderItemId);

        orderItem.MarkReceivedByCustomer();

        RaiseDomainEvent(new OrderItemDelivered(Id, orderItemId, Guid.Empty));
    }
    private IReadOnlyCollection<OrderReservationItem> GetReservationItems()
        => _orderItems
            .GroupBy(i => i.ProductId)
            .Select(g => new OrderReservationItem(g.Key, g.Sum(i => i.Quantity)))
            .ToList();

    #endregion
}

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
    public Order(string code, Guid customerId, decimal orderTotal, Guid? createdByUserId) : base(Guid.NewGuid())
    {
        (Code, CustomerId, OrderTotal, CreatedByUserId) = (code, customerId, orderTotal, createdByUserId);

        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; }
    public Guid CustomerId { get; private set; }
    public Guid? CreatedByUserId { get; }

    public decimal OrderTotal { get; private set; }
    public decimal OrderDiscount { get; internal set; }

    public DateTime ExpectedShippingDateUtc { get; set; }

    public PaymentStatus PaymentStatus { get; private set; }
    public PaymentMethod? PaymentMethod { get; private set; }
    public DateTime? PaidOnUtc { get; private set; }
    public string? PaymentNote { get; private set; }

    public ShippingStatus ShippingStatus { get; private set; }
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
    public DateTime? ShippedOnUtc { get; private set; }
    public string? ShippingNote { get; private set; }

    public OrderStatus OrderStatus { get; private set; }
    public string? CancellationReason { get; private set; }


    private readonly List<OrderItem> _orderItems = [];
    public IEnumerable<OrderItem> OrderItems => _orderItems.AsReadOnly();

    public string? Note { get; internal set; }

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

    private void RecalculateTotal()
    {
        OrderTotal = _orderItems.Sum(i => i.Price) - OrderDiscount;
    }

    internal bool CanUpdateInfo() => OrderStatus != OrderStatus.Completed && OrderStatus != OrderStatus.Cancelled;
    internal bool CanChangeStatusTo(OrderStatus toStatus)
    {
        if (!CanUpdateInfo())
            return false;

        if (toStatus == OrderStatus.Cancelled && (PaymentStatus != PaymentStatus.Pending || ShippingStatus != ShippingStatus.Pending))
            return false;

        var subtract = (int)toStatus - (int)OrderStatus;
        if (subtract < 0)
            return false;

        return Enum.IsDefined(toStatus);
    }
    internal bool CanUpdateOrderItems()
    {
        if (!CanUpdateInfo())
            return false;

        if (PaymentStatus == PaymentStatus.Paid)
            return false;

        if (ShippingStatus == ShippingStatus.Shipped)
            return false;

        return true;
    }

    internal void ChangeStatus(OrderStatus status)
    {
        if (!CanChangeStatusTo(status))
            throw new OrderCannotChangeStatusException();

        OrderStatus = status;
    }

    internal void MarkAsPaid(PaymentMethod method, string? note)
    {
        if (!CanUpdateInfo())
            throw new OrderCancelledException();

        if (!Enum.IsDefined(method))
            throw new OrderDataIsInvalidException("Payment method is not allowed.");

        if (PaymentStatus == PaymentStatus.Paid)
            throw new OrderIsAlreadyPaidException();

        PaymentStatus = PaymentStatus.Paid;
        PaymentMethod = method;
        PaymentNote = note;

        PaidOnUtc = DateTime.UtcNow;
    }

    internal void UpdateShipping(ShippingStatus status, string? address, string? note)
    {
        if (!CanUpdateInfo())
            throw new OrderCannotUpdateInfoException();

        if (!Enum.IsDefined(status))
            throw new OrderDataIsInvalidException("Shipping status is not allowed.");

        if (ShippingStatus == ShippingStatus.Shipped)
            throw new OrderIsAlreadyShippedException();

        ShippingStatus = status;
        ShippingNote = note;
        if (address is not null)
            ShippingAddress = address;

        if (status == ShippingStatus.Shipped)
            ShippedOnUtc = DateTime.UtcNow;
    }

    internal bool VerifyStatus()
    {
        if (!CanUpdateInfo())
            return false;

        if (PaymentStatus == PaymentStatus.Paid && ShippingStatus == ShippingStatus.Shipped)
        {
            ChangeStatus(OrderStatus.Completed);
            return true;
        }

        if (OrderStatus == OrderStatus.Pending && OrderItems.Any() && ShippingStatus == ShippingStatus.Shipping)
        {
            ChangeStatus(OrderStatus.Processing);
            return true;
        }
        if (OrderStatus == OrderStatus.Pending && PaymentStatus == PaymentStatus.Paid)
        {
            ChangeStatus(OrderStatus.Processing);
            return true;
        }

        return false;
    }

    internal void Cancel(string? reason)
    {
        if (OrderStatus == OrderStatus.Cancelled)
            throw new OrderCancelledException();

        if (!CanChangeStatusTo(OrderStatus.Cancelled))
            throw new OrderCannotBeCancelledException();

        ChangeStatus(OrderStatus.Cancelled);
        CancellationReason = reason;
    }

    #endregion
}

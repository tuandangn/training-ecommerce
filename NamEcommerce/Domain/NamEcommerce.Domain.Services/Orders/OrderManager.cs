using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Customers;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Users;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Domain.Services.Orders;

public sealed class OrderManager : IOrderManager
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IEntityDataReader<Order> _orderDataReader;
    private readonly IInventoryStockManager _stockManager;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<Customer> _customerDataReader;
    private readonly IEventPublisher _eventPublisher;
    private readonly IEntityDataReader<User> _userDataReader;

    public OrderManager(IRepository<Order> orderRepository, IEntityDataReader<Order> orderDataReader,
        IInventoryStockManager stockManager, IEntityDataReader<Product> productDataReader,
        IEntityDataReader<Customer> customerDataReader, IEventPublisher eventPublisher,
        IEntityDataReader<User> userDataReader)
    {
        _orderRepository = orderRepository;
        _orderDataReader = orderDataReader;
        _stockManager = stockManager;
        _productDataReader = productDataReader;
        _customerDataReader = customerDataReader;
        _eventPublisher = eventPublisher;
        _userDataReader = userDataReader;
    }

    public async Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        if (await DoesCodeExistAsync(dto.Code).ConfigureAwait(false))
            throw new PurchaseOrderCodeExistsException(dto.Code);

        var customer = await _customerDataReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer is null)
            throw new CustomerIsNotFoundException(dto.CustomerId);

        if (dto.CreatedByUserId.HasValue)
        {
            var user = await _userDataReader.GetByIdAsync(dto.CreatedByUserId.Value).ConfigureAwait(false);
            if (user is null)
                throw new UserIsNotFoundException(dto.CreatedByUserId.Value);
        }

        var orderTotal = dto.Items.Sum(item => item.UnitPrice * item.Quantity);
        var order = new Order(dto.Code, dto.CustomerId, orderTotal, dto.CreatedByUserId)
        {
            OrderDiscount = dto.OrderDiscount ?? 0,
            Note = dto.Note,
            ShippingAddress = customer.Address,
            ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc
        };

        foreach (var item in dto.Items)
            await order.AddOrderItemAsync(item.ProductId, item.UnitPrice, item.Quantity, _productDataReader).ConfigureAwait(false);

        var insertedOrder = await _orderRepository.InsertAsync(order).ConfigureAwait(false);

        await _eventPublisher.EntityCreated(insertedOrder).ConfigureAwait(false);

        return new CreateOrderResultDto { CreatedId = insertedOrder.Id };
    }

    public async Task<UpdateOrderResultDto> UpdateOrderAsync(UpdateOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var order = await _orderDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.Id);

        if (!order.CanUpdateInfo())
            throw new OrderCannotUpdateInfoException();

        order.ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc;
        order.OrderDiscount = dto.OrderDiscount ?? 0;
        order.Note = dto.Note;

        order.UpdatedOnUtc = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);

        return new UpdateOrderResultDto { UpdatedId = updatedOrder.Id };
    }

    public async Task AddOrderItemAsync(Guid orderId, AddOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var order = await _orderDataReader.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);

        var product = await _productDataReader.GetByIdAsync(dto.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(dto.ProductId);

        await order.AddOrderItemAsync(dto.ProductId, dto.UnitPrice, dto.Quantity, _productDataReader).ConfigureAwait(false);

        order.UpdatedOnUtc = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);
    }

    public async Task UpdateOrderItemAsync(UpdateOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await _orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        order.UpdateOrderItem(dto.OrderItemId, dto.Quantity, dto.UnitPrice);
        order.UpdatedOnUtc = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);
    }

    public async Task DeleteOrderItemAsync(DeleteOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await _orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        if (!order.CanUpdateOrderItems())
            throw new OrderCannotUpdateOrderItemsException();

        order.RemoveOrderItem(dto.OrderItemId);
        order.UpdatedOnUtc = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);
    }

    public async Task ChangeOrderStatusAsync(Guid orderId, OrderStatus status)
    {
        var order = await _orderDataReader.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);

        order.ChangeStatus(status);
        order.UpdatedOnUtc = DateTime.UtcNow;

        //*TODO*
        //// Inventory Integration
        //if (order.WarehouseId.HasValue)
        //{
        //    if (toStatus == OrderStatus.Completed)
        //    {
        //        // Dispatch stock (Deduct)
        //        foreach (var item in order.OrderItems)
        //        {
        //            await _stockManager.DispatchStockAsync(item.ProductId, order.WarehouseId.Value, item.Quantity, order.Id, Guid.Empty, $"Sales Order {order.Id} Completed").ConfigureAwait(false);
        //        }
        //    }
        //    else if (toStatus == OrderStatus.Cancelled && fromStatus != OrderStatus.Completed)
        //    {
        //        // Release reservation
        //        foreach (var item in order.OrderItems)
        //        {
        //            await _stockManager.ReleaseReservedStockAsync(item.ProductId, order.WarehouseId.Value, item.Quantity, order.Id, Guid.Empty, "Sales Order Cancelled").ConfigureAwait(false);
        //        }
        //    }
        //}

        var updatedOrder = await _orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);
    }

    public async Task VerifyStatusAsync(Guid orderId)
    {
        var order = await _orderDataReader.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);

        var hasChanged = order.VerifyStatus();

        if (!hasChanged)
            return;

        order.UpdatedOnUtc = DateTime.UtcNow;
        await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task MarkAsPaidAsync(MarkAsPaidDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await _orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        order.MarkAsPaid(dto.PaymentMethod, dto.Note);
        order.UpdatedOnUtc = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);
    }

    public async Task UpdateShippingAsync(UpdateShippingDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await _orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        order.UpdateShipping(dto.ShippingStatus, dto.Address, dto.Note);
        order.UpdatedOnUtc = DateTime.UtcNow;

        //*TODO*
        //if (order.OrderStatus == OrderStatus.Completed && order.WarehouseId.HasValue)
        //{
        //    // Only if it wasn't already completed
        //    // Wait, UpdateShipping internally calls ChangeStatus(Completed) if Shipped.
        //    // But we need to check if the old status was not Completed to dispatch stock.
        //    // Let's rely on the fact that if it transitions to completed, we dispatch it.
        //    // Actually Order.UpdateShipping already changed it. We don't have the old status nicely here, 
        //    // but we know it throws if already completed or cancelled, so this is the exact transition point.
        //    foreach (var item in order.OrderItems)
        //    {
        //        await _stockManager.DispatchStockAsync(item.ProductId, order.WarehouseId.Value, item.Quantity, order.Id, Guid.Empty, $"Sales Order {order.Id} Completed on Shipping").ConfigureAwait(false);
        //    }
        //}

        var updatedOrder = await _orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);
    }

    public async Task CancelOrderAsync(CancelOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await _orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        order.Cancel(dto.Reason);
        order.UpdatedOnUtc = DateTime.UtcNow;

        //*TODO*
        //if (order.WarehouseId.HasValue)
        //{
        //    // Release reservation
        //    foreach (var item in order.OrderItems)
        //    {
        //        await _stockManager.ReleaseReservedStockAsync(item.ProductId, order.WarehouseId.Value, item.Quantity, order.Id, Guid.Empty, "Sales Order Cancelled").ConfigureAwait(false);
        //    }
        //}

        var updatedOrder = await _orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid id)
    {
        var order = await _orderDataReader.GetByIdAsync(id).ConfigureAwait(false);

        if (order is null)
            return null;
        return order.ToDto();
    }

    public async Task<IPagedDataDto<OrderDto>> GetOrdersAsync(string? keywords, OrderStatus? status, int pageIndex, int pageSize)
    {
        var query = _orderDataReader.DataSource;

        if (status.HasValue)
            query = query.Where(order => order.OrderStatus == status);

        if (!string.IsNullOrEmpty(keywords))
        {
            var normalizedKeywords = TextHelper.Normalize(keywords);

            var customerIds = _customerDataReader.DataSource
                .Where(c => c.FullName.Contains(keywords) || c.FullName.Contains(normalizedKeywords) || c.NormalizedFullName.Contains(normalizedKeywords)
                    || c.Address.Contains(keywords) || c.Address.Contains(normalizedKeywords) || c.NormalizedAddress.Contains(normalizedKeywords)
                    || c.PhoneNumber.Contains(keywords))
                .Select(v => v.Id)
                .ToList()
                .OfType<Guid?>()
                .ToList();
            IList<Guid?> userIds = [];

            query = query.Where(order => order.Code.Contains(keywords) || order.Code.Contains(normalizedKeywords)
                || (order.ShippingAddress != null && (order.ShippingAddress.Contains(keywords) || order.ShippingAddress.Contains(normalizedKeywords) || order.NormalizedShippingAddress.Contains(normalizedKeywords)))
                || customerIds.Contains(order.CustomerId)
                || userIds.Contains(order.CreatedByUserId));
        }

        var total = query.Count();
        var data = query
            .OrderByDescending(o => o.CreatedOnUtc)
            .ThenByDescending(o => o.ExpectedShippingDateUtc)
            .Skip(pageIndex * pageSize).Take(pageSize)
            .ToList();

        var pagedData = PagedDataDto.Create(data.Select(order => order.ToDto()), pageIndex, pageSize, total);
        return pagedData;
    }

    public Task<bool> DoesCodeExistAsync(string code, Guid? comparesWithCurrentId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);

        var query = from purchaseOrder in _orderDataReader.DataSource
                    where purchaseOrder.Code == code && (comparesWithCurrentId == null || purchaseOrder.Id != comparesWithCurrentId)
                    select purchaseOrder;

        var sameNameExists = query.FirstOrDefault() != null;
        return Task.FromResult(sameNameExists);
    }
}

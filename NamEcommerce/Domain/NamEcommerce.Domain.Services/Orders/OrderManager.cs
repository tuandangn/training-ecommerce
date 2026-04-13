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
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Domain.Services.Orders;

public sealed class OrderManager(
    IRepository<Order> orderRepository, 
    IEntityDataReader<Order> orderDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<Customer> customerDataReader,
    IEntityDataReader<User> userDataReader,
    IEventPublisher eventPublisher) : IOrderManager
{

    public async Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        if (await DoesCodeExistAsync(dto.Code).ConfigureAwait(false))
            throw new PurchaseOrderCodeExistsException(dto.Code);

        var customer = await customerDataReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer is null)
            throw new CustomerIsNotFoundException(dto.CustomerId);

        if (dto.CreatedByUserId.HasValue)
        {
            var user = await userDataReader.GetByIdAsync(dto.CreatedByUserId.Value).ConfigureAwait(false);
            if (user is null)
                throw new UserIsNotFoundException(dto.CreatedByUserId.Value);
        }

        var order = new Order(dto.Code)
        {
            CreatedByUserId = dto.CreatedByUserId,
            Note = dto.Note,
            ShippingAddress = string.IsNullOrEmpty(dto.ShippingAddress) ? customer.Address : dto.ShippingAddress,
            ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc
        };
        await order.SetCustomerAsync(dto.CustomerId, customerDataReader).ConfigureAwait(false);
        foreach (var item in dto.Items)
            await order.AddOrderItemAsync(item.ProductId, item.UnitPrice, item.Quantity, productDataReader).ConfigureAwait(false);
        order.SetOrderDiscount(dto.OrderDiscount);

        var insertedOrder = await orderRepository.InsertAsync(order).ConfigureAwait(false);

        await eventPublisher.EntityCreated(insertedOrder).ConfigureAwait(false);

        return new CreateOrderResultDto { CreatedId = insertedOrder.Id };
    }

    public async Task<UpdateOrderResultDto> UpdateOrderAsync(UpdateOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var order = await orderDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.Id);

        if (!order.CanUpdateInfo())
            throw new OrderCannotUpdateInfoException();

        order.ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc;
        order.SetOrderDiscount(dto.OrderDiscount);
        order.Note = dto.Note;

        order.UpdatedOnUtc = DateTime.UtcNow;

        var updatedOrder = await orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);

        return new UpdateOrderResultDto { UpdatedId = updatedOrder.Id };
    }

    public async Task AddOrderItemAsync(Guid orderId, AddOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var order = await orderDataReader.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);

        var product = await productDataReader.GetByIdAsync(dto.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(dto.ProductId);

        await order.AddOrderItemAsync(dto.ProductId, dto.UnitPrice, dto.Quantity, productDataReader).ConfigureAwait(false);

        order.UpdatedOnUtc = DateTime.UtcNow;

        var updatedOrder = await orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);
    }

    public async Task UpdateOrderItemAsync(UpdateOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        order.UpdateOrderItem(dto.OrderItemId, dto.Quantity, dto.UnitPrice);
        order.UpdatedOnUtc = DateTime.UtcNow;

        var updatedOrder = await orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);
    }

    public async Task DeleteOrderItemAsync(DeleteOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        if (!order.CanUpdateOrderItems())
            throw new OrderCannotUpdateOrderItemsException();

        order.RemoveOrderItem(dto.OrderItemId);
        order.UpdatedOnUtc = DateTime.UtcNow;

        var updatedOrder = await orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);
    }

    public async Task LockOrderAsync(LockOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        order.LockOrder(dto.Reason);
        order.UpdatedOnUtc = DateTime.UtcNow;

        var updatedOrder = await orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid id)
    {
        var order = await orderDataReader.GetByIdAsync(id).ConfigureAwait(false);

        if (order is null)
            return null;
        return order.ToDto();
    }

    public async Task<IPagedDataDto<OrderDto>> GetOrdersAsync(string? keywords, OrderStatus? status, int pageIndex, int pageSize)
    {
        var query = orderDataReader.DataSource;

        if (status.HasValue)
            query = query.Where(order => order.OrderStatus == status);

        if (!string.IsNullOrEmpty(keywords))
        {
            var normalizedKeywords = TextHelper.Normalize(keywords);

            var customerIds = customerDataReader.DataSource
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

        var query = from purchaseOrder in orderDataReader.DataSource
                    where purchaseOrder.Code == code && (comparesWithCurrentId == null || purchaseOrder.Id != comparesWithCurrentId)
                    select purchaseOrder;

        var sameNameExists = query.FirstOrDefault() != null;
        return Task.FromResult(sameNameExists);
    }

    public async Task UpdateShippingAsync(UpdateShippingDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        order.ShippingAddress = dto.Address;
        order.UpdatedOnUtc = DateTime.UtcNow;

        var updatedOrder = await orderRepository.UpdateAsync(order).ConfigureAwait(false);

        await eventPublisher.EntityUpdated(updatedOrder).ConfigureAwait(false);
    }
}

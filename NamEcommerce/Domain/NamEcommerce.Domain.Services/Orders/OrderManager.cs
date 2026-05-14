using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Customers;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Domain.Services.Orders;

public sealed class OrderManager(
    IRepository<Order> orderRepository,
    IEntityDataReader<Order> orderDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<Customer> customerDataReader,
    IEntityDataReader<DeliveryNote> deliveryNoteDataReader,
    IInventoryStockManager stockManager,
    ICurrentUserAccessor currentUserAccessor) : IOrderManager
{
    private Task<string> GenerateCodeAsync()
    {
        var monthPrefix = $"DB-{DateTime.UtcNow:yyMM}";
        var count = ((EntityDataReader<Order>)orderDataReader).SecuredDataSource.Count(d => d.Code.StartsWith(monthPrefix));
        return Task.FromResult($"{monthPrefix}-{(count + 1):D3}");
    }

    public async Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var customer = await customerDataReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer is null)
            throw new CustomerIsNotFoundException(dto.CustomerId);

        var code = await GenerateCodeAsync().ConfigureAwait(false);
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var order = new Order(code, currentUser)
        {
            Note = dto.Note,
            ShippingAddress = string.IsNullOrEmpty(dto.ShippingAddress) ? customer.Address : dto.ShippingAddress,
            ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc
        };
        await order.SetCustomerAsync(dto.CustomerId, customerDataReader).ConfigureAwait(false);
        foreach (var item in dto.Items)
            await order.AddOrderItemAsync(item.ProductId, item.UnitPrice, item.Quantity, productDataReader).ConfigureAwait(false);
        order.SetOrderDiscount(dto.OrderDiscount);
        await EnsureAvailableForProductsWithoutVendorAsync(order.OrderItems.Select(item => (item.ProductId, item.Quantity))).ConfigureAwait(false);

        order.ClearDomainEvents();
        order.Place();

        var insertedOrder = await orderRepository.InsertAsync(order).ConfigureAwait(false);

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
        order.MarkInfoUpdated();

        var updatedOrder = await orderRepository.UpdateAsync(order).ConfigureAwait(false);

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

        await EnsureAvailableForProductsWithoutVendorAsync([(dto.ProductId, dto.Quantity)]).ConfigureAwait(false);
        await order.AddOrderItemAsync(dto.ProductId, dto.UnitPrice, dto.Quantity, productDataReader).ConfigureAwait(false);

        order.UpdatedOnUtc = DateTime.UtcNow;

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task UpdateOrderItemAsync(UpdateOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        var deliveryNoteOrderItems = (from deliveryNote in deliveryNoteDataReader.DataSource
                                      where deliveryNote.OrderId == order.Id && deliveryNote.Items.Any(item => item.OrderItemId == dto.OrderItemId)
                                         && deliveryNote.Status != DeliveryNoteStatus.Cancelled
                                      select deliveryNote)
                                     .SelectMany(deliveryNote => deliveryNote.Items.Where(item => item.OrderItemId == dto.OrderItemId))
                                     .ToList();
        var deliveryNoteQty = deliveryNoteOrderItems.Sum(item => item.Quantity);
        if (dto.Quantity < deliveryNoteQty)
            throw new InvalidOperationException("Updated order item quantity cannot less than its delivering quantity.");
        if (deliveryNoteOrderItems.Any(item => item.UnitPrice != dto.UnitPrice))
            throw new InvalidOperationException("Updated order item cannot change unit price of items that are already in delivery notes.");

        var currentItem = order.OrderItems.FirstOrDefault(item => item.Id == dto.OrderItemId);
        if (currentItem is null)
            throw new OrderItemIsNotFoundException(dto.OrderItemId);

        var deltaQuantity = dto.Quantity - currentItem.Quantity;
        if (deltaQuantity > 0)
            await EnsureAvailableForProductsWithoutVendorAsync([(currentItem.ProductId, deltaQuantity)]).ConfigureAwait(false);

        order.UpdateOrderItem(dto.OrderItemId, dto.Quantity, dto.UnitPrice);
        order.UpdatedOnUtc = DateTime.UtcNow;

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task DeleteOrderItemAsync(DeleteOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        if (!order.CanUpdateOrderItems())
            throw new OrderCannotUpdateOrderItemsException();

        var orderItemDeliveryNotes = from deliveryNote in deliveryNoteDataReader.DataSource
                                     where deliveryNote.OrderId == order.Id && deliveryNote.Items.Any(item => item.OrderItemId == dto.OrderItemId)
                                        && deliveryNote.Status != DeliveryNoteStatus.Cancelled
                                     select deliveryNote;
        if (orderItemDeliveryNotes.Any())
            throw new OrderCannotUpdateOrderItemsException();

        order.RemoveOrderItem(dto.OrderItemId);
        order.UpdatedOnUtc = DateTime.UtcNow;

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task LockOrderAsync(LockOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        order.LockOrder(dto.Reason);
        order.UpdatedOnUtc = DateTime.UtcNow;

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid id)
    {
        var order = await orderDataReader.GetByIdAsync(id).ConfigureAwait(false);

        if (order is null)
            return null;
        return order.ToDto();
    }

    public Task<IPagedDataDto<OrderDto>> GetOrdersAsync(int pageIndex, int pageSize, string? keywords, OrderStatus? status)
        => GetOrdersAsync(pageIndex, pageSize, keywords, status.HasValue ? [status.Value] : []);

    public async Task<IPagedDataDto<OrderDto>> GetOrdersAsync(int pageIndex, int pageSize, string? keywords, IEnumerable<OrderStatus> status)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0, nameof(pageIndex));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0, nameof(pageSize));

        var query = orderDataReader.DataSource;

        if (status != null && status.Any())
            query = query.Where(order => status.Contains(order.OrderStatus));

        if (!string.IsNullOrEmpty(keywords))
        {
            var normalizedKeywords = TextHelper.Normalize(keywords);
            var uppercaseKeywords = keywords.Trim().ToUpper();

            var customerIds = customerDataReader.DataSource
                .Where(c => c.FullName.ToUpper().Contains(uppercaseKeywords) || c.FullName.ToUpper().Contains(normalizedKeywords) || c.NormalizedFullName.Contains(normalizedKeywords)
                    || c.Address.ToUpper().Contains(uppercaseKeywords) || c.Address.ToUpper().Contains(normalizedKeywords) || c.NormalizedAddress.Contains(normalizedKeywords)
                    || c.PhoneNumber.Contains(keywords))
                .Select(v => v.Id)
                .ToList()
                .OfType<Guid?>()
                .ToList();
            IList<Guid?> userIds = [];

            query = query.Where(order => order.Code.Contains(keywords) || order.Code.Contains(normalizedKeywords)
                || (order.ShippingAddress != null && (order.ShippingAddress.ToUpper().Contains(uppercaseKeywords) || order.ShippingAddress.ToUpper().Contains(normalizedKeywords) || order.NormalizedShippingAddress.Contains(normalizedKeywords)))
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

        dto.Verify();

        var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        if (dto.ExpectedShippingDateUtc.HasValue)
            order.ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc.Value;
        order.ShippingAddress = dto.Address;
        order.UpdatedOnUtc = DateTime.UtcNow;
        order.MarkShippingUpdated();

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task MarkOrderItemDeliveredAsync(MarkOrderItemDeliveredDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        order.MarkOrderItemDelivered(dto.OrderItemId, dto.PictureId);
        order.TryAutoLock();
        order.UpdatedOnUtc = DateTime.UtcNow;

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task DeleteOrderAsync(DeleteOrderDto dto)
    {
        var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        if (!order.CanUpdateInfo())
            throw new InvalidOperationException("Order cannot delete.");

        var activeDeliveryNotes = from deliveryNote in deliveryNoteDataReader.DataSource
                                  where deliveryNote.OrderId == order.Id && deliveryNote.Status != DeliveryNoteStatus.Cancelled
                                  select deliveryNote;
        if (activeDeliveryNotes.Any())
            throw new InvalidOperationException("Order cannot deleted because it is processing.");

        order.MarkDeleted();

        await orderRepository.DeleteAsync(order).ConfigureAwait(false);
    }

    public async Task CancelOrderAsync(CancelOrderDto dto)
    {
        var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        var activeDeliveryNotes = from deliveryNote in deliveryNoteDataReader.DataSource
                                  where deliveryNote.OrderId == order.Id && deliveryNote.Status != DeliveryNoteStatus.Cancelled
                                  select deliveryNote;
        if (activeDeliveryNotes.Any())
            throw new InvalidOperationException("Order cannot cancelled because it is processing.");

        order.Cancel();
        order.UpdatedOnUtc = DateTime.UtcNow;

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    private async Task EnsureAvailableForProductsWithoutVendorAsync(IEnumerable<(Guid ProductId, decimal Quantity)> items)
    {
        foreach (var itemGroup in items.GroupBy(item => item.ProductId))
        {
            var product = await productDataReader.GetByIdAsync(itemGroup.Key).ConfigureAwait(false);
            if (product is null)
                throw new ProductIsNotFoundException(itemGroup.Key);

            if (product.ProductVendors.Any())
                continue;

            var requestedQuantity = itemGroup.Sum(item => item.Quantity);
            var availableQuantity = await stockManager.GetGlobalAvailableForProductAsync(itemGroup.Key).ConfigureAwait(false);
            if (availableQuantity < requestedQuantity)
                throw new InsufficientStockException(itemGroup.Key, Guid.Empty, requestedQuantity, availableQuantity);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Customers;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Customers;
using NamEcommerce.Domain.Shared.Exceptions.Debts;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Domain.Shared.Specifications;
using NamEcommerce.Domain.Specifications.Customers;
using NamEcommerce.Domain.Specifications.Orders;

namespace NamEcommerce.Domain.Services.Orders;

public sealed class OrderManager(
    IRepository<Order> orderRepository,
    IEntityDataReader<Order> orderDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<Customer> customerDataReader,
    IEntityDataReader<DeliveryNote> deliveryNoteDataReader,
    IProductReservationManager productReservationManager,
    IEntityDataReader<PurchaseOrderItemAllocation> allocationDataReader,
    IInventoryStockManager stockManager,
    ICurrentUserAccessor currentUserAccessor,
    EntityCodeGenerator entityCodeGenerator,
    IEntityDataReader<OrderFulfillmentSchedule> scheduleDataReader,
    IRepository<OrderFulfillmentScheduleItem> scheduleItemRepository,
    IBankTransferPaymentIntentManager bankTransferPaymentIntentManager) : IOrderManager
{
    public Task<bool> DoesCodeExistAsync(string code, Guid? comparesWithCurrentId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);

        var query = from purchaseOrder in orderDataReader.DataSource
                    where purchaseOrder.Code == code && (comparesWithCurrentId == null || purchaseOrder.Id != comparesWithCurrentId)
                    select purchaseOrder;

        return query.AnyAsync();
    }


    public async Task<OrderDto?> GetOrderByIdAsync(Guid id)
    {
        var order = await orderRepository.GetByIdAsync(id).ConfigureAwait(false);
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

        var specification = new CompositeSpecification<Order>();
        if (!string.IsNullOrEmpty(keywords))
        {
            specification.Or(new OrderKeywordSearchSpec(keywords));

            var matchedCustomers = await customerDataReader.GetListAsync(new CustomerKeywordSearchSpec(keywords)).ConfigureAwait(false);
            var customerIds = matchedCustomers.Select(v => v.Id).OfType<Guid?>().ToArray();
            specification.Or(new OrdersOfBuyersSpec(customerIds));
        }

        specification.ApplyOrderByDescending(order => order.CreatedOnUtc);

        var total = await orderDataReader.CountAsync(specification).ConfigureAwait(false);
        var pagedData = await orderDataReader.GetPagedListAsync(specification, pageIndex, pageSize).ConfigureAwait(false);

        return PagedDataDto.Create(pagedData.Select(order => order.ToDto()), pageIndex, pageSize, total);
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
            ShippingPhoneNumber = string.IsNullOrWhiteSpace(dto.ShippingPhoneNumber) ? customer.PhoneNumber : dto.ShippingPhoneNumber.Trim(),
            ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc
        };
        await order.SetCustomerAsync(dto.CustomerId, customerDataReader).ConfigureAwait(false);
        foreach (var item in dto.Items)
            await order.AddOrderItemAsync(item.ProductId, item.UnitPrice, item.Quantity, productDataReader).ConfigureAwait(false);
        order.SetOrderDiscount(dto.OrderDiscount);
        if (customer.Kind == CustomerKind.RetailWalkIn && order.OrderTotal > 0)
            order.RequiresPayment(dto.RequiresPayOff);

        order.ClearDomainEvents();
        order.Place();

        var insertedOrder = await orderRepository.InsertAsync(order).ConfigureAwait(false);

        foreach (var itemGroup in order.OrderItems.GroupBy(item => item.ProductId))
        {
            await productReservationManager.ReserveAsync(
                itemGroup.Key, itemGroup.Sum(item => item.Quantity),
                order.Id, ProductReservationReason.OrderCreated, order.Id).ConfigureAwait(false);
        }

        return new CreateOrderResultDto { CreatedId = insertedOrder.Id };
    }

    public async Task<UpdateOrderResultDto> UpdateOrderAsync(UpdateOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var order = await orderRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
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

    public async Task DeleteOrderAsync(DeleteOrderDto dto)
    {
        var order = await orderRepository.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        var canDeleteOrder = order.OrderStatus is OrderStatus.Pending or OrderStatus.Cancelled;
        if (!canDeleteOrder)
            throw new InvalidOperationException("Order cannot delete.");

        var activeDeliveryNotes = await (from deliveryNote in deliveryNoteDataReader.DataSource
                                         where deliveryNote.OrderId == order.Id
                                            && deliveryNote.Status != DeliveryNoteStatus.Cancelled
                                         select deliveryNote).AnyAsync().ConfigureAwait(false);
        if (activeDeliveryNotes)
            throw new InvalidOperationException("Order cannot deleted because it is processing.");

        order.MarkDeleted();

        await orderRepository.DeleteAsync(order).ConfigureAwait(false);
    }


    public async Task UpdateShippingAsync(UpdateShippingDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var order = await orderRepository.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        if (dto.ExpectedShippingDateUtc.HasValue)
            order.ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc.Value;
        order.ShippingAddress = dto.Address;
        order.ShippingPhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        order.UpdatedOnUtc = DateTime.UtcNow;
        order.MarkShippingUpdated();

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task RequestDeliveryAsync(Guid orderId, Guid deliveryNoteId, DateTime requestedAtUtc)
    {
        var order = await orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);

        order.RequestDelivered(deliveryNoteId, requestedAtUtc);
        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }
    public async Task MarkOrderHasPayment(Guid orderId, decimal paidAmount, Guid? paymentIntentId)
    {
        var order = await orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);

        if (order.HadPaid())
            throw new OrderIsPaidException(orderId);

        if (paymentIntentId.HasValue)
        {
            var paymentIntent = await bankTransferPaymentIntentManager.GetByIdAsync(paymentIntentId.Value).ConfigureAwait(false);
            if (paymentIntent is null)
                throw new PaymentIntentIsNotFoundException(paymentIntentId.Value);

            if (paymentIntent.CustomerId.HasValue && paymentIntent.CustomerId.Value != order.CustomerId)
                throw new PaymentIntentCustomerIsMismatchException();

            if (paymentIntent.Amount != paidAmount)
                throw new PaymentIntentCustomerIsMismatchException();
        }

        order.OrderHasPayment(paidAmount, paymentIntentId);
    }

    public async Task CompleteOrderAsync(CompleteOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderRepository.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        if (!order.OrderItems.Any() || !order.OrderItems.All(i => i.IsDelivered))
            throw new OrderCannotChangeStatusException();

        order.Complete();
        order.UpdatedOnUtc = DateTime.UtcNow;

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task CancelOrderAsync(CancelOrderDto dto)
    {
        var order = await orderRepository.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        var activeDeliveryNotes = await (from deliveryNote in deliveryNoteDataReader.DataSource
                                         where deliveryNote.OrderId == order.Id && deliveryNote.Status != DeliveryNoteStatus.Cancelled
                                         select deliveryNote).AnyAsync().ConfigureAwait(false);
        if (activeDeliveryNotes)
            throw new InvalidOperationException("Order cannot cancelled because it is processing.");

        order.Cancel();
        order.UpdatedOnUtc = DateTime.UtcNow;

        if (dto.FullyReceivedAllocationIds.Count > 0)
            order.RaiseSoCancelledWithDirectShipReceived(dto.FullyReceivedAllocationIds);

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }


    public async Task AddOrderItemAsync(Guid orderId, AddOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var order = await orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);

        var product = await productDataReader.GetByIdAsync(dto.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(dto.ProductId);

        await EnsureAvailableForProductsWithoutVendorAsync([(dto.ProductId, dto.Quantity)]).ConfigureAwait(false);
        var orderItem = await order.AddOrderItemAsync(dto.ProductId, dto.UnitPrice, dto.Quantity, productDataReader).ConfigureAwait(false);

        order.UpdatedOnUtc = DateTime.UtcNow;

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);

        order.MarkAddAdded(orderItem);
    }

    public async Task UpdateOrderItemAsync(UpdateOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var order = await orderRepository.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        var deliveryNoteOrderItems = await (from deliveryNote in deliveryNoteDataReader.DataSource
                                            where deliveryNote.OrderId == order.Id && deliveryNote.Items.Any(item => item.OrderItemId == dto.OrderItemId)
                                               && deliveryNote.Status != DeliveryNoteStatus.Cancelled
                                            select deliveryNote)
                                     .SelectMany(deliveryNote => deliveryNote.Items.Where(item => item.OrderItemId == dto.OrderItemId))
                                     .ToListAsync().ConfigureAwait(false);
        var deliveryNoteQty = deliveryNoteOrderItems.Sum(item => item.Quantity);
        var outstandingAllocationQty = await GetOutstandingAllocationQuantityAsync(dto.OrderItemId).ConfigureAwait(false);
        if (dto.Quantity < deliveryNoteQty)
            throw new InvalidOperationException("Updated order item quantity cannot less than its delivering quantity.");
        if (dto.Quantity < deliveryNoteQty + outstandingAllocationQty)
            throw new OrderCannotUpdateOrderItemsException();
        if (deliveryNoteOrderItems.Any(item => item.UnitPrice != dto.UnitPrice))
            throw new InvalidOperationException("Updated order item cannot change unit price of items that are already in delivery notes.");

        var currentItem = order.OrderItems.FirstOrDefault(item => item.Id == dto.OrderItemId);
        if (currentItem is null)
            throw new OrderItemIsNotFoundException();

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

        var order = await orderRepository.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        if (!order.CanUpdateOrderItems())
            throw new OrderCannotUpdateOrderItemsException();

        var hasOrderItemDeliveryNotes = await (from deliveryNote in deliveryNoteDataReader.DataSource
                                               where deliveryNote.OrderId == order.Id && deliveryNote.Items.Any(item => item.OrderItemId == dto.OrderItemId)
                                                  && deliveryNote.Status != DeliveryNoteStatus.Cancelled
                                               select deliveryNote).AnyAsync().ConfigureAwait(false);
        if (hasOrderItemDeliveryNotes)
            throw new OrderCannotUpdateOrderItemsException();

        if (await HasReceivedAllocationsAsync(dto.OrderItemId).ConfigureAwait(false))
            throw new OrderCannotUpdateOrderItemsException();

        var scheduleItemsToDelete = await scheduleDataReader.DataSource
            .Where(s => s.OrderId == order.Id)
            .SelectMany(s => s.Items)
            .Where(item => item.OrderItemId == dto.OrderItemId)
            .ToListAsync().ConfigureAwait(false);
        foreach (var scheduleItem in scheduleItemsToDelete)
            await scheduleItemRepository.DeleteAsync(scheduleItem).ConfigureAwait(false);

        order.RemoveOrderItem(dto.OrderItemId);
        order.UpdatedOnUtc = DateTime.UtcNow;

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }


    public async Task MarkOrderItemDeliveredAsync(MarkOrderItemDeliveredDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderRepository.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        order.MarkOrderItemDelivered(dto.OrderItemId, dto.PictureId);
        order.UpdatedOnUtc = DateTime.UtcNow;
        order.RaiseFullyDeliveredIfComplete();

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task MarkOrderItemReceivedByCustomerAsync(MarkOrderItemReceivedByCustomerDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderRepository.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        order.MarkOrderItemReceivedByCustomer(dto.OrderItemId);
        order.UpdatedOnUtc = DateTime.UtcNow;

        await orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }


    public async Task<IList<RecentSalePriceDto>> GetRecentSalePricesAsync(Guid productId, Guid customerId, int take = 10)
    {
        if (productId == Guid.Empty || customerId == Guid.Empty)
            return [];

        var safeTake = Math.Max(1, take);
        var prices = await orderDataReader.DataSource
            .Where(order => order.OrderStatus != OrderStatus.Cancelled
                && order.CustomerId == customerId
                && order.OrderItems.Any(item => item.ProductId == productId))
            .OrderByDescending(order => order.CreatedOnUtc)
            .Take(safeTake)
            .SelectMany(order => order.OrderItems
                .Where(item => item.ProductId == productId)
                .Select(item => new RecentSalePriceDto(
                    CustomerId: order.CustomerId,
                    CustomerName: order.CustomerInfo.FullName,
                    UnitPrice: item.UnitPrice,
                    OrderCode: order.Code,
                    OrderDateUtc: order.CreatedOnUtc)))
            .Take(safeTake)
            .ToListAsync().ConfigureAwait(false);

        return prices;
    }


    #region Helper methods

    private Task<string> GenerateCodeAsync()
    {
        var prefix = $"{Order.CODE_PREFIX}-{DateTime.UtcNow:yyMM}";
        return entityCodeGenerator.NextAsync(prefix, () => orderDataReader.SecuredDataSource.CountAsync(d => d.Code.StartsWith(prefix)));
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
            var availableQuantity = await stockManager.GetGlobalAvailableQuantityForProductAsync(itemGroup.Key).ConfigureAwait(false);
            if (availableQuantity < requestedQuantity)
                throw new InsufficientStockException(itemGroup.Key, Guid.Empty, requestedQuantity, availableQuantity);
        }
    }

    private Task<decimal> GetOutstandingAllocationQuantityAsync(Guid orderItemId)
        => allocationDataReader.DataSource
            .Where(allocation => allocation.OrderItemId.SecondaryId == orderItemId
                && allocation.Status != AllocationStatus.Cancelled)
            .SumAsync(allocation => Math.Max(0m, allocation.AllocatedQuantity - allocation.ReceivedQuantity));

    private Task<bool> HasReceivedAllocationsAsync(Guid orderItemId)
        => allocationDataReader.DataSource
            .AnyAsync(allocation => allocation.OrderItemId.SecondaryId == orderItemId
                && allocation.Status != AllocationStatus.Cancelled
                && allocation.ReceivedQuantity > 0);

    #endregion
}

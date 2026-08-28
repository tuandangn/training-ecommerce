using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.Returns;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Specifications;
using NamEcommerce.Domain.Specifications.DeliveryNotes;
using NamEcommerce.Domain.Specifications.Orders;
using NamEcommerce.Domain.Specifications.PurchaseOrders;

namespace NamEcommerce.Domain.Services.Inventory;

public sealed class ShortageQueryService(
    IInventoryStockManager inventoryStockManager,
    IEntityDataReader<Order> orderReader,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IEntityDataReader<PurchaseOrder> purchaseOrderReader,
    IEntityDataReader<CustomerReturn> customerReturnReader,
    IEntityDataReader<PurchaseOrderItemAllocation> allocationReader,
    IRepository<Order> orderRepository, IRepository<DeliveryNote> deliveryNoteRepository) : IShortageQueryService
{
    private static readonly DeliveryNoteStatus[] ShippedStatuses =
    [
        DeliveryNoteStatus.Confirmed,
        DeliveryNoteStatus.Delivering,
        DeliveryNoteStatus.PendingConfirmation,
        DeliveryNoteStatus.Delivered
    ];

    private static readonly PurchaseOrderStatus[] ActivePurchaseOrderStatuses =
    [
        PurchaseOrderStatus.Draft,
        PurchaseOrderStatus.Submitted,
        PurchaseOrderStatus.Approved,
        PurchaseOrderStatus.Receiving
    ];

    public async Task<IList<OrderItemShortageDto>> GetOrderItemShortagesAsync(Guid orderId)
    {
        var order = await orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);
        if (!order.CanProcess())
            return [];

        var shortages = await BuildOrderItemShortagesAsync(order, null).ConfigureAwait(false);

        return shortages.Where(x => x.ShortageQuantity > 0).ToList();
    }

    public async Task<IList<OrderItemFulfillmentStateDto>> GetOrderItemFulfillmentStatesAsync(Guid orderId)
    {
        var order = await orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);

        return await BuildOrderItemFulfillmentStatesAsync(order, null).ConfigureAwait(false);
    }

    public async Task<IList<DeliveryNoteItemShortageDto>> GetDeliveryNoteShortagesAsync(Guid deliveryNoteId)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(deliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(deliveryNoteId);

        if (deliveryNote.Status != DeliveryNoteStatus.Draft || deliveryNote.OrderId == Guid.Empty)
            return [];

        var order = await orderRepository.GetByIdAsync(deliveryNote.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(deliveryNote.OrderId);

        var orderItemIds = deliveryNote.Items
            .Where(item => item.OrderItemId != Guid.Empty)
            .Select(item => (SecondaryItemId)(deliveryNote.OrderId, item.OrderItemId))
            .ToList();
        var shippedQuantities = await GetShippedQuantitiesAsync(orderItemIds, deliveryNote.Id, order.Id).ConfigureAwait(false);
        var allocationsByOrderItem = await GetPurchaseOrderAllocationsAsync(orderItemIds).ConfigureAwait(false);
        var result = new List<DeliveryNoteItemShortageDto>();

        foreach (var productGroup in deliveryNote.Items
                     .Where(item => item.OrderItemId != Guid.Empty)
                     .GroupBy(item => item.ProductId))
        {
            var availableForOrder = await inventoryStockManager
                .ComputeAvailableQuantityForOrderAsync(productGroup.Key, deliveryNote.OrderId)
                .ConfigureAwait(false);
            // The delivery note's quantities are already warehouse-reserved (order-level reservation
            // was released and transferred to warehouse-level in DeliveryNoteCreatedHandler).
            // Add them back so the shortage reflects actual missing stock, not the warehouse reservation.
            var warehouseReservedForThisDn = productGroup.Sum(item => item.Quantity);
            var remainingAvailable = availableForOrder + warehouseReservedForThisDn;

            foreach (var item in productGroup)
            {
                var requiredQuantity = item.Quantity;
                var availableQuantity = Math.Min(requiredQuantity, remainingAvailable);
                remainingAvailable -= availableQuantity;

                var allocations = GetAllocationsForOrderItem(allocationsByOrderItem, item.OrderItemId);
                var allocatedIncoming = allocations.Sum(allocation => allocation.AllocatedQty - allocation.ReceivedQty);

                result.Add(new DeliveryNoteItemShortageDto
                {
                    OrderId = order.Id,
                    OrderCode = order.Code,
                    DeliveryNoteItemId = item.Id,
                    OrderItemId = item.OrderItemId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    RequiredQuantity = requiredQuantity,
                    ShippedQuantity = shippedQuantities.GetValueOrDefault(item.OrderItemId),
                    AvailableQuantity = availableQuantity,
                    ShortageQuantity = Math.Max(0, requiredQuantity - availableQuantity - allocatedIncoming),
                    AllocatedFromPurchaseOrders = allocations,
                    ShippingAddress = order.ShippingAddress,
                    ShippingPhoneNumber = order.ShippingPhoneNumber
                });
            }
        }

        return result.Where(x => x.ShortageQuantity > 0).ToList();
    }

    public async Task<IList<OrderItemShortageDto>> GetGlobalShortagesAsync(ShortageFilterDto? filter)
    {
        if (filter?.OrderId is Guid orderId)
        {
            var orderShortages = await GetOrderItemShortagesAsync(orderId).ConfigureAwait(false);
            return ApplyFilter(orderShortages, filter).ToList();
        }

        if (filter?.DeliveryNoteId is Guid deliveryNoteId)
        {
            var deliveryNote = await deliveryNoteRepository.GetByIdAsync(deliveryNoteId).ConfigureAwait(false);
            if (deliveryNote is null)
                throw new DeliveryNoteNotFoundException(deliveryNoteId);
            if (deliveryNote.OrderId == Guid.Empty)
                return [];

            var order = await orderRepository.GetByIdAsync(deliveryNote.OrderId).ConfigureAwait(false);
            if (order is null)
                throw new OrderIsNotFoundException(deliveryNote.OrderId);
            if (!order.CanProcess())
                return [];

            var itemIds = deliveryNote.Items
                .Where(item => item.OrderItemId != Guid.Empty)
                .Select(item => item.OrderItemId)
                .ToHashSet();
            var deliveryNoteShortages = await BuildOrderItemShortagesAsync(order, itemIds).ConfigureAwait(false);

            return ApplyFilter(deliveryNoteShortages.Where(x => x.ShortageQuantity > 0), filter).ToList();
        }

        var orderSpecification = new CompositeSpecification<Order>(new NotHaveStatusOrderSpec([OrderStatus.Completed, OrderStatus.Cancelled]))
            .AndNot(new IsPaymentRequiredOrderSpec());
        orderSpecification.ApplyOrderBy(order => order.CreatedOnUtc);

        var orders = await orderReader.GetPagedListAsync(orderSpecification, 0, int.MaxValue).ConfigureAwait(false);

        var result = new List<OrderItemShortageDto>();
        foreach (var order in orders)
        {
            var shortages = await BuildOrderItemShortagesAsync(order, null).ConfigureAwait(false);
            result.AddRange(shortages.Where(x => x.ShortageQuantity > 0));
        }

        return ApplyFilter(result, filter).ToList();
    }

    private async Task<List<OrderItemShortageDto>> BuildOrderItemShortagesAsync(Order order, ISet<Guid>? limitedOrderItemIds)
    {
        var fulfillmentStates = await BuildOrderItemFulfillmentStatesAsync(order, limitedOrderItemIds).ConfigureAwait(false);
        return fulfillmentStates
            .Select(state => new OrderItemShortageDto
            {
                OrderId = state.OrderId,
                OrderCode = state.OrderCode,
                OrderItemId = state.OrderItemId,
                ProductId = state.ProductId,
                ProductName = state.ProductName,
                RequiredQuantity = state.RequiredQuantity,
                ShippedQuantity = state.ShippedQuantity,
                AvailableQuantity = state.AvailableQuantity,
                ShortageQuantity = state.MissingSourceQuantity,
                CustomerName = state.CustomerName,
                CustomerPhone = state.CustomerPhone,
                CustomerAddress = state.CustomerAddress,
                ShippingAddress = state.ShippingAddress,
                ShippingPhoneNumber = state.ShippingPhoneNumber,
                AllocatedFromPurchaseOrders = state.AllocatedFromPurchaseOrders
            }).ToList();
    }

    private async Task<IList<OrderItemFulfillmentStateDto>> BuildOrderItemFulfillmentStatesAsync(Order order, ISet<Guid>? limitedOrderItemIds)
    {
        var orderItems = order.OrderItems
            .Where(item => limitedOrderItemIds is null || limitedOrderItemIds.Contains(item.Id))
            .ToList();
        var orderItemIds = orderItems.Select(item => (SecondaryItemId)(order.Id, item.Id)).ToList();

        var shippedQuantities = await GetShippedQuantitiesAsync(orderItemIds, null, order.Id).ConfigureAwait(false);
        var allocationsByOrderItem = await GetPurchaseOrderAllocationsAsync(orderItemIds).ConfigureAwait(false);

        var result = new List<OrderItemFulfillmentStateDto>();
        foreach (var productGroup in orderItems.GroupBy(item => item.ProductId))
        {
            var remainingAvailable = await inventoryStockManager
                .ComputeAvailableQuantityForOrderAsync(productGroup.Key, order.Id)
                .ConfigureAwait(false);

            foreach (var item in productGroup)
            {
                var shippedQuantity = Math.Min(item.Quantity, shippedQuantities.GetValueOrDefault(item.Id));
                var stillNeeded = Math.Max(0, item.Quantity - shippedQuantity);
                var availableQuantity = Math.Min(stillNeeded, remainingAvailable);
                remainingAvailable -= availableQuantity;

                var allocations = GetAllocationsForOrderItem(allocationsByOrderItem, item.Id);
                var allocatedIncoming = allocations.Sum(allocation => allocation.AllocatedQty - allocation.ReceivedQty);

                result.Add(new OrderItemFulfillmentStateDto
                {
                    OrderId = order.Id,
                    OrderCode = order.Code,
                    OrderItemId = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName ?? string.Empty,
                    RequiredQuantity = item.Quantity,
                    ShippedQuantity = shippedQuantity,
                    AvailableQuantity = availableQuantity,
                    AllocatedIncomingQuantity = allocatedIncoming,
                    MissingSourceQuantity = Math.Max(0, stillNeeded - availableQuantity - allocatedIncoming),
                    CustomerName = order.CustomerInfo.FullName,
                    CustomerPhone = order.CustomerInfo.PhoneNumber,
                    CustomerAddress = order.CustomerInfo.Address,
                    ShippingAddress = order.ShippingAddress,
                    ShippingPhoneNumber = order.ShippingPhoneNumber,
                    AllocatedFromPurchaseOrders = allocations
                });
            }
        }

        return result;
    }

    private async Task<Dictionary<Guid, decimal>> GetShippedQuantitiesAsync(IList<SecondaryItemId> orderItemIds, Guid? excludedDeliveryNoteId, Guid orderId)
    {
        var activeDeliveryNotesSpec = new CompositeSpecification<DeliveryNote>(new HaveStatusDeliveryNoteSpec(ShippedStatuses));

        if (excludedDeliveryNoteId.HasValue)
            activeDeliveryNotesSpec.AndNot(new HaveIdsDeliveryNoteSpec([excludedDeliveryNoteId.Value]));

        var itemIds = orderItemIds.Select(id => id.SecondaryId).ToList();
        activeDeliveryNotesSpec.And(new DeliveryNotesOfOrderItemsSpec(orderId, itemIds));

        var shippedQuantities = await deliveryNoteReader.ApplySpecification(activeDeliveryNotesSpec)
            .SelectMany(note => note.Items)
            .GroupBy(item => item.OrderItemId)
            .ToDictionaryAsync(group => group.Key, group => group.Sum(item => item.Quantity)).ConfigureAwait(false);

        var returnedQuantities = await GetCompensatedReturnedQuantities(orderId, orderItemIds).ConfigureAwait(false);
        foreach (var id in orderItemIds)
        {
            var shippedQuantity = shippedQuantities.GetValueOrDefault(id.SecondaryId);
            var returnedQuantity = returnedQuantities.GetValueOrDefault(id.SecondaryId);
            shippedQuantities[id.SecondaryId] = Math.Max(0m, shippedQuantity - returnedQuantity);
        }

        return shippedQuantities;
    }

    private async Task<Dictionary<Guid, decimal>> GetCompensatedReturnedQuantities(Guid orderId, IList<SecondaryItemId> orderItemIds)
    {
        if (orderItemIds.Count == 0)
            return [];

        var itemIds = orderItemIds.Select(id => id.SecondaryId).ToList();
        var validDeliveryNoteItems = deliveryNoteReader.ApplySpecification(new ActiveDeliveryNotesOfOrderItemsSpec(orderId, itemIds))
            .SelectMany(note => note.Items)
            .Select(item => new { item.Id, item.OrderItemId });

        return await customerReturnReader.DataSource
            .Where(returnNote => returnNote.Status != CustomerReturnStatus.Cancelled
                && returnNote.CompensateInNextDelivery
                && returnNote.Status != CustomerReturnStatus.Draft)
            .SelectMany(returnNote => returnNote.Items)
            .Where(returnItem => returnItem.DeliveryNoteItemId.HasValue)
            .Join(
                validDeliveryNoteItems,
                returnItem => returnItem.DeliveryNoteItemId!.Value,
                deliveryNoteItem => deliveryNoteItem.Id,
                (returnItem, deliveryNoteItem) => new { deliveryNoteItem.OrderItemId, returnItem.AcceptedQuantity })
            .GroupBy(item => item.OrderItemId)
            .ToDictionaryAsync(group => group.Key, group => group.Sum(item => item.AcceptedQuantity)).ConfigureAwait(false);
    }

    private async Task<Dictionary<Guid, List<PurchaseOrderShortageAllocationDto>>> GetPurchaseOrderAllocationsAsync(IEnumerable<SecondaryItemId> orderItemIds)
    {
        var itemIds = orderItemIds.Select(id => id.SecondaryId).Distinct().ToList();
        if (itemIds.Count == 0)
            return [];

        var allocations = await allocationReader.ApplySpecification(new ActivePurchaseOrderAllocationOfOrderItemSpec(orderItemIds.First().PrimaryId, itemIds))
            .OrderBy(allocation => allocation.CreatedOnUtc)
            .ToListAsync().ConfigureAwait(false);
        if (allocations.Count == 0)
            return [];

        var purchaseOrderItemIds = allocations
            .Select(allocation => allocation.PurchaseOrderItemId.SecondaryId)
            .Distinct()
            .ToList();
        var activePurchaseOrderSpec = new CompositeSpecification<PurchaseOrder>(new HaveStatusPurchaseOrderSpec(ActivePurchaseOrderStatuses));
        activePurchaseOrderSpec.And(new PurchaseOrdersOfPurchaseOrderItemsSpec(purchaseOrderItemIds));

        var purchaseOrderItemMap = await purchaseOrderReader.ApplySpecification(activePurchaseOrderSpec)
            .SelectMany(purchaseOrder => purchaseOrder.Items
                .Where(item => purchaseOrderItemIds.Contains(item.Id))
                .Select(item => new
                {
                    PurchaseOrderItemId = item.Id,
                    PurchaseOrderId = purchaseOrder.Id,
                    PurchaseOrderCode = purchaseOrder.Code,
                    purchaseOrder.ExpectedDeliveryDateUtc
                }))
            .ToDictionaryAsync(item => item.PurchaseOrderItemId).ConfigureAwait(false);

        var result = new Dictionary<Guid, List<PurchaseOrderShortageAllocationDto>>();
        foreach (var allocation in allocations)
        {
            if (!purchaseOrderItemMap.TryGetValue(allocation.PurchaseOrderItemId.SecondaryId, out var purchaseOrderItem))
                continue;

            if (!result.TryGetValue(allocation.OrderItemId.SecondaryId, out var orderItemAllocations))
            {
                orderItemAllocations = [];
                result[allocation.OrderItemId.SecondaryId] = orderItemAllocations;
            }

            orderItemAllocations.Add(new PurchaseOrderShortageAllocationDto
            {
                POId = purchaseOrderItem.PurchaseOrderId,
                POCode = purchaseOrderItem.PurchaseOrderCode,
                AllocatedQty = allocation.AllocatedQuantity,
                ReceivedQty = allocation.ReceivedQuantity,
                ExpectedReceiveDateUtc = purchaseOrderItem.ExpectedDeliveryDateUtc,
                IsDirectShip = allocation.IsDirectShip
            });
        }

        return result;
    }

    private static IList<PurchaseOrderShortageAllocationDto> GetAllocationsForOrderItem(
        Dictionary<Guid, List<PurchaseOrderShortageAllocationDto>> allocationsByOrderItem, Guid orderItemId)
        => allocationsByOrderItem.TryGetValue(orderItemId, out var allocations) ? allocations : [];

    private static IEnumerable<OrderItemShortageDto> ApplyFilter(IEnumerable<OrderItemShortageDto> shortages, ShortageFilterDto? filter)
    {
        if (filter?.ProductId is Guid productId)
            return shortages.Where(x => x.ProductId == productId);

        return shortages;
    }
}

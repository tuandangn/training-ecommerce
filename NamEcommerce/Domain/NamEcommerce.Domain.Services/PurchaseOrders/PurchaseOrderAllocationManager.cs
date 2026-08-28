using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;
using NamEcommerce.Domain.Specifications.Orders;
using NamEcommerce.Domain.Specifications.PurchaseOrders;
using NamEcommerce.Domain.Specifications.DeliveryNotes;

namespace NamEcommerce.Domain.Services.PurchaseOrders;

public sealed class PurchaseOrderAllocationManager(
    IRepository<PurchaseOrderItemAllocation> allocationRepository,
    IEntityDataReader<PurchaseOrderItemAllocation> allocationReader,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IEntityDataReader<PurchaseOrder> purchaseOrderReader,
    IRepository<PurchaseOrder> purchaseOrderRepository,
    IEntityDataReader<Order> orderReader,
    IEntityDataReader<Vendor> vendorReader,
    IEntityDataReader<Product> productReader) : IPurchaseOrderAllocationManager
{
    public async Task<PurchaseOrderItemAllocationDto> AllocatePurchaseOrderItemForOrder(AllocatePurchaseOrderItemForOrder dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var (purchaseOrder, purchaseOrderItem) = await EnsurePurchaseOrderItemExists(dto.PurchaseOrderItemId);
        var isValidPurchaseOrderStatus = purchaseOrder.Status
            is PurchaseOrderStatus.Draft
            or PurchaseOrderStatus.Submitted
            or PurchaseOrderStatus.Approved
            or PurchaseOrderStatus.Receiving;
        if (!isValidPurchaseOrderStatus)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemCannotAllocate");

        var (order, orderItem) = await EnsureOrderItemExists(dto.OrderItemId);
        await EnsureOrderItemCanAllocateAsync(orderItem, purchaseOrderItem.ProductId, dto.AllocationQuantity).ConfigureAwait(false);

        var allocations = await allocationReader.GetListAsync(
            new ActivePurchaseOrderAllocationOfPurchaseOrderItemSpec(dto.PurchaseOrderItemId.PrimaryId, [dto.PurchaseOrderItemId.SecondaryId]),
            new() { ReadWrite = true }).ConfigureAwait(false);
        var allocatedQuantity = allocations.Sum(allocation => allocation.AllocatedQuantity);
        var availableForAllocation = purchaseOrderItem.QuantityOrdered - allocatedQuantity;
        if (dto.AllocationQuantity > availableForAllocation)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemAllocationQuantityExceedsAvailable");

        var allocation = new PurchaseOrderItemAllocation(dto.PurchaseOrderItemId, dto.OrderItemId, dto.AllocationQuantity);
        if (dto.DirectShipInfo is not null)
        {
            var (contactName, contactPhone, address) = dto.DirectShipInfo;
            allocation.SetDirectShip(address ?? string.Empty, contactName, contactPhone, dto.DirectShipInfo.Priority);
        }
        var inserted = await allocationRepository.InsertAsync(allocation).ConfigureAwait(false);

        return inserted.ToDto();
    }

    public async Task<DistributeReceivedQuantityResultDto> SyncReceivedForPurchaseOrderItemAsync(Guid purchaseOrderItemId, decimal receivedQuantity)
    {
        if (purchaseOrderItemId == Guid.Empty)
            throw new PurchaseOrderItemIsNotFoundException();
        if (receivedQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(receivedQuantity));

        var allocations = await allocationReader.ApplySpecification(new ActivePurchaseOrderAllocationOfPurchaseOrderItemSpec([purchaseOrderItemId]), new() { ReadWrite = true })
            .OrderBy(allocation => allocation.CreatedOnUtc)
            .ToListAsync()
            .ConfigureAwait(false);
        if (allocations.Count == 0)
            return EmptyDistributeResult();

        var totalAllocated = allocations.Sum(allocation => allocation.AllocatedQuantity);
        var currentReceived = allocations.Sum(allocation => allocation.ReceivedQuantity);
        var targetReceived = Math.Min(receivedQuantity, totalAllocated);
        var quantityToDistribute = targetReceived - currentReceived;
        if (quantityToDistribute == 0)
            return EmptyDistributeResult();
        if (quantityToDistribute < 0)
        {
            await ReduceReceivedForAllocationsAsync(allocations, Math.Abs(quantityToDistribute)).ConfigureAwait(false);
            return EmptyDistributeResult();
        }

        var directShipReceipts = new List<AllocationReceiptDto>();
        var warehouseReceipts = new List<AllocationReceiptDto>();

        foreach (var allocation in allocations)
        {
            var remainingAllocation = allocation.AllocatedQuantity - allocation.ReceivedQuantity;
            if (remainingAllocation <= 0)
                continue;

            var receivedForAllocation = Math.Min(remainingAllocation, quantityToDistribute);
            allocation.IncreaseReceived(receivedForAllocation);
            await allocationRepository.UpdateAsync(allocation).ConfigureAwait(false);

            var receipt = new AllocationReceiptDto
            {
                AllocationId = allocation.Id,
                OrderItemId = allocation.OrderItemId,
                Quantity = receivedForAllocation,
                IsDirectShip = allocation.IsDirectShip,
                DirectShipAddress = allocation.DirectShipAddress
            };
            if (allocation.IsDirectShip)
                directShipReceipts.Add(receipt);
            else
                warehouseReceipts.Add(receipt);

            quantityToDistribute -= receivedForAllocation;
            if (quantityToDistribute <= 0)
                break;
        }

        return new DistributeReceivedQuantityResultDto
        {
            DirectShipReceipts = directShipReceipts,
            WarehouseReceipts = warehouseReceipts
        };
    }

    public async Task<IList<PurchaseOrderItemAllocationDto>> GetAllocationsForPurchaseOrderItemAsync(Guid purchaseOrderItemId)
    {
        var allocations = await allocationReader.ApplySpecification(new ActivePurchaseOrderAllocationOfPurchaseOrderItemSpec([purchaseOrderItemId]))
            .OrderBy(allocation => allocation.CreatedOnUtc)
            .ToListAsync()
            .ConfigureAwait(false);

        return allocations.Select(allocation => allocation.ToDto()).ToList();
    }

    public async Task ReleaseAllocationsOfPurchaseOrderItemAsync(SecondaryItemId purchaseOrderItemId)
    {
        var allocations = await allocationReader.ApplySpecification(new PurchaseOrderAllocationOfPurchaseOrderItemsSpec([purchaseOrderItemId.SecondaryId]), new() { ReadWrite = true })
            .Where(allocation => allocation.Status != AllocationStatus.Cancelled)
            .ToListAsync().ConfigureAwait(false);

        await ReleaseAllocations(allocations).ConfigureAwait(false);
    }

    public async Task ReleaseAllocationsForOrderAsync(Guid orderId)
    {
        if (orderId == Guid.Empty)
            return;

        var orderItemIds = await orderReader.GetDataSource(new() { ReadWrite = true })
            .Where(order => order.Id == orderId)
            .SelectMany(order => order.OrderItems.Select(item => item.Id))
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var orderItemId in orderItemIds)
            await ReleaseAllocationsForOrderItemAsync(orderItemId).ConfigureAwait(false);
    }

    public async Task ReleaseAllocationsForOrderItemAsync(Guid orderItemId)
    {
        if (orderItemId == Guid.Empty)
            return;

        var allocations = await allocationReader.GetDataSource(new() { ReadWrite = true })
            .Where(allocation => allocation.OrderItemId.SecondaryId == orderItemId && allocation.Status != AllocationStatus.Cancelled)
            .ToListAsync()
            .ConfigureAwait(false);

        await ReleaseAllocations(allocations).ConfigureAwait(false);
    }

    private async Task ReleaseAllocations(IEnumerable<PurchaseOrderItemAllocation> allocations)
    {
        foreach (var allocation in allocations)
        {
            if (allocation.ReceivedQuantity == 0)
            {
                await allocationRepository.DeleteAsync(allocation).ConfigureAwait(false);
                continue;
            }
            if (allocation.AllocatedQuantity > allocation.ReceivedQuantity)
            {
                allocation.ReduceAllocationToReceived();
                await allocationRepository.UpdateAsync(allocation).ConfigureAwait(false);
            }
        }
    }

    public async Task<IList<OrderAllocatedPurchaseOrderDto>> GetAllocatedPurchaseOrdersForOrderAsync(Guid orderId)
    {
        var order = await orderReader.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            return [];

        var orderItemIds = order.OrderItems.Select(item => item.Id).ToList();
        if (orderItemIds.Count == 0)
            return [];

        var allocations = await allocationReader.ApplySpecification(new PurchaseOrderAllocationOfOrderItemsSpec(order.Id, orderItemIds))
            .OrderBy(allocation => allocation.CreatedOnUtc)
            .ToListAsync().ConfigureAwait(false);
        if (allocations.Count == 0)
            return [];

        var purchaseOrderItemIds = allocations.Select(allocation => allocation.PurchaseOrderItemId.SecondaryId).Distinct().ToList();
        var purchaseOrders = await purchaseOrderReader.ApplySpecification(new PurchaseOrdersOfPurchaseOrderItemsSpec(purchaseOrderItemIds))
            .Where(purchaseOrder => purchaseOrder.Status != PurchaseOrderStatus.Cancelled)
            .ToListAsync().ConfigureAwait(false);

        var poItemToPurchaseOrder = purchaseOrders
            .SelectMany(purchaseOrder => purchaseOrder.Items
                .Where(item => purchaseOrderItemIds.Contains(item.Id))
                .Select(item => new
                {
                    PurchaseOrderItemId = (SecondaryItemId)(purchaseOrder.Id, item.Id),
                    PurchaseOrder = purchaseOrder,
                    Item = item
                }))
            .ToDictionary(entry => entry.PurchaseOrderItemId);

        var productIds = poItemToPurchaseOrder.Values.Select(entry => entry.Item.ProductId).Distinct().ToList();
        var products = productReader.DataSource
            .Where(product => productIds.Contains(product.Id))
            .ToDictionary(product => product.Id, product => product.Name);

        var vendorIds = purchaseOrders.Select(purchaseOrder => purchaseOrder.VendorId).Distinct().ToList();
        var vendors = vendorReader.DataSource
            .Where(vendor => vendorIds.Contains(vendor.Id))
            .ToDictionary(vendor => vendor.Id, vendor => vendor.Name);

        var groups = new Dictionary<Guid, (PurchaseOrder PurchaseOrder, List<OrderAllocatedPurchaseOrderItemDto> Items)>();
        foreach (var allocation in allocations)
        {
            if (!poItemToPurchaseOrder.TryGetValue(allocation.PurchaseOrderItemId, out var entry))
                continue;

            if (!groups.TryGetValue(entry.PurchaseOrder.Id, out var groupItems))
            {
                groupItems = (entry.PurchaseOrder, []);
                groups[entry.PurchaseOrder.Id] = groupItems;
            }

            groupItems.Items.Add(new OrderAllocatedPurchaseOrderItemDto
            {
                OrderItemId = allocation.OrderItemId,
                ProductId = entry.Item.ProductId,
                ProductName = products.TryGetValue(entry.Item.ProductId, out var productName) ? productName : string.Empty,
                AllocatedQuantity = allocation.AllocatedQuantity,
                ReceivedQuantity = allocation.ReceivedQuantity
            });
        }

        var result = groups.Values
            .Select(group => new OrderAllocatedPurchaseOrderDto
            {
                PurchaseOrderId = group.PurchaseOrder.Id,
                PurchaseOrderCode = group.PurchaseOrder.Code,
                Status = group.PurchaseOrder.Status,
                VendorId = group.PurchaseOrder.VendorId,
                VendorName = vendors.TryGetValue(group.PurchaseOrder.VendorId, out var vendorName) ? vendorName : string.Empty,
                PlacedOnUtc = group.PurchaseOrder.PlacedOnUtc,
                ExpectedDeliveryDateUtc = group.PurchaseOrder.ExpectedDeliveryDateUtc,
                Items = group.Items
            })
            .OrderByDescending(dto => dto.PlacedOnUtc)
            .ToList();

        return result;
    }

    public async Task<IList<EligibleOrderItemForAllocationDto>> GetEligibleOrderItemsForPoItemAsync(SecondaryItemId purchaseOrderItemId)
    {
        if (!purchaseOrderItemId.IsValid())
            return [];

        var purchaseOrder = await purchaseOrderReader.GetByIdAsync(purchaseOrderItemId.PrimaryId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return [];

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => purchaseOrderItemId.SecondaryId == item.Id);
        if (purchaseOrderItem is null)
            return [];

        var productId = purchaseOrderItem.ProductId;
        var eligibleItems = await orderReader.ApplySpecification(new NotHaveStatusOrderSpec([OrderStatus.Cancelled]))
            .SelectMany(order => order.OrderItems
                .Where(item => item.ProductId == productId)
                .Select(item => new { Order = order, Item = item }))
            .ToListAsync().ConfigureAwait(false);
        if (eligibleItems.Count == 0)
            return [];

        var eligibleOrderItemIds = eligibleItems.Select(ctx => ctx.Item.Id).Distinct().ToList();
        var allocatedOutstandingByOrderItemId = await allocationReader
            .ApplySpecification(new ActivePurchaseOrderAllocationOfOrderItemSpec(eligibleOrderItemIds))
            .GroupBy(a => a.OrderItemId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(a => Math.Max(0m, a.AllocatedQuantity - a.ReceivedQuantity))).ConfigureAwait(false);

        var activeDeliveryQuantitiesByOrderItemId = await GetActiveDeliveryQuantitiesAsync(eligibleOrderItemIds).ConfigureAwait(false);

        return eligibleItems
            .Select(ctx =>
            {
                var outstanding = allocatedOutstandingByOrderItemId.TryGetValue((SecondaryItemId)(ctx.Order.Id, ctx.Item.Id), out var v) ? v : 0m;
                var activeDeliveryQuantity = activeDeliveryQuantitiesByOrderItemId.GetValueOrDefault(ctx.Item.Id);
                return new EligibleOrderItemForAllocationDto
                {
                    OrderItemId = ctx.Item.Id,
                    OrderId = ctx.Order.Id,
                    OrderCode = ctx.Order.Code,
                    CustomerName = ctx.Order.CustomerInfo.FullName,
                    CustomerPhone = ctx.Order.CustomerInfo.PhoneNumber,
                    ProductId = ctx.Item.ProductId,
                    ProductName = ctx.Item.ProductName ?? string.Empty,
                    TotalQuantity = ctx.Item.Quantity,
                    AllocatedOutstanding = outstanding,
                    AvailableToAllocate = Math.Max(0m, ctx.Item.Quantity - activeDeliveryQuantity - outstanding),
                    ShippingContactName = ctx.Order.CustomerInfo.FullName,
                    ShippingAddress = ctx.Order.ShippingAddress,
                    ShippingPhoneNumber = ctx.Order.ShippingPhoneNumber
                };
            })
            .Where(dto => dto.AvailableToAllocate > 0)
            .OrderBy(dto => dto.OrderCode)
            .ToList();
    }

    public async Task<IList<NonDirectShipAllocationDto>> GetNonDirectShipAllocationsForPoItemAsync(SecondaryItemId purchaseOrderItemId)
    {
        var allocations = await allocationReader.ApplySpecification(new ActivePurchaseOrderAllocationOfPurchaseOrderItemSpec(purchaseOrderItemId.PrimaryId, [purchaseOrderItemId.SecondaryId]))
            .Where(a => !a.IsDirectShip && a.AllocatedQuantity > a.ReceivedQuantity)
            .ToListAsync().ConfigureAwait(false);
        if (allocations.Count == 0)
            return [];

        var orderItemIds = allocations.Select(a => a.OrderItemId).ToHashSet();
        var orderIds = orderItemIds.Select(id => id.PrimaryId).Distinct().ToList();

        var orders = await orderReader.GetByIdsAsync(orderIds).ConfigureAwait(false);
        var orderItems = orders
            .SelectMany(order => order.OrderItems
                .Where(item => orderItemIds.Any(itemId => itemId.SecondaryId == item.Id))
                .Select(item => new { Order = order, Item = item }))
            .ToDictionary(x => (SecondaryItemId)(x.Order.Id, x.Item.Id));

        var result = allocations
            .Where(allocation => orderItems.ContainsKey(allocation.OrderItemId))
            .Select(allocation =>
            {
                var ctx = orderItems[allocation.OrderItemId];
                return new NonDirectShipAllocationDto
                {
                    AllocationId = allocation.Id,
                    OrderId = ctx.Order.Id,
                    OrderItemId = allocation.OrderItemId.SecondaryId,
                    OrderCode = ctx.Order.Code,
                    CustomerName = ctx.Order.CustomerInfo.FullName,
                    CustomerPhone = ctx.Order.CustomerInfo.PhoneNumber,
                    AllocatedQuantity = allocation.AllocatedQuantity,
                    RemainingQuantity = Math.Max(0m, allocation.AllocatedQuantity - allocation.ReceivedQuantity),
                    ShippingContactName = ctx.Order.CustomerInfo.FullName,
                    ShippingAddress = ctx.Order.ShippingAddress,
                    ShippingPhoneNumber = ctx.Order.ShippingPhoneNumber
                };
            })
            .ToList();

        return result;
    }

    private async Task<(PurchaseOrder, PurchaseOrderItem)> EnsurePurchaseOrderItemExists(SecondaryItemId purchaseOrderItemId)
    {
        var purchaseOrder = await purchaseOrderRepository.GetByIdAsync(purchaseOrderItemId.PrimaryId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderItemIsNotFoundException();

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == purchaseOrderItemId.SecondaryId);
        if (purchaseOrderItem is null)
            throw new PurchaseOrderItemIsNotFoundException();

        return (purchaseOrder, purchaseOrderItem);
    }

    private async Task<(Order, OrderItem)> EnsureOrderItemExists(SecondaryItemId orderItemId)
    {
        var order = await orderReader.GetByIdAsync(orderItemId.PrimaryId).ConfigureAwait(false);
        if (order is null)
            throw new OrderItemIsNotFoundException();

        var orderItem = order.OrderItems.FirstOrDefault(item => item.Id == orderItemId.SecondaryId);
        if (orderItem is null)
            throw new OrderItemIsNotFoundException();

        return (order, orderItem);
    }

    private async Task EnsureOrderItemCanAllocateAsync(OrderItem orderItem, Guid productId, decimal quantity)
    {
        if (orderItem.ProductId != productId)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemAllocationProductMismatch");

        var allocatedOutstanding = (await allocationReader.GetListAsync(new ActivePurchaseOrderAllocationOfOrderItemSpec([orderItem.Id]), new() { ReadWrite = true }).ConfigureAwait(false))
            .Sum(allocation => Math.Max(0m, allocation.AllocatedQuantity - allocation.ReceivedQuantity));
        var activeDeliveryQuantity = await GetActiveDeliveryQuantityAsync(orderItem.Id).ConfigureAwait(false);
        var availableQuantity = orderItem.Quantity - activeDeliveryQuantity - allocatedOutstanding;
        if (quantity > availableQuantity)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemAllocationQuantityExceedsAvailable");
    }

    private async Task<decimal> GetActiveDeliveryQuantityAsync(Guid orderItemId)
    {
        var qtyMap = await GetActiveDeliveryQuantitiesAsync([orderItemId]);
        return qtyMap.GetValueOrDefault(orderItemId);
    }

    private async Task<Dictionary<Guid, decimal>> GetActiveDeliveryQuantitiesAsync(IList<Guid> orderItemIds)
    {
        if (orderItemIds.Count == 0)
            return [];

        return await deliveryNoteReader.ApplySpecification(new ActiveDeliveryNotesOfOrderItemsSpec(orderItemIds), new() { ReadWrite = true })
            .SelectMany(deliveryNote => deliveryNote.Items)
            .Where(item => orderItemIds.Contains(item.OrderItemId))
            .GroupBy(item => item.OrderItemId)
            .ToDictionaryAsync(group => group.Key, group => group.Sum(item => item.Quantity)).ConfigureAwait(false);
    }

    private static DistributeReceivedQuantityResultDto EmptyDistributeResult()
        => new()
        {
            DirectShipReceipts = [],
            WarehouseReceipts = []
        };

    private async Task ReduceReceivedForAllocationsAsync(IList<PurchaseOrderItemAllocation> allocations, decimal quantityToReduce)
    {
        foreach (var allocation in allocations
            .Where(allocation => allocation.ReceivedQuantity > 0)
            .OrderBy(allocation => allocation.IsDirectShip)
            .ThenBy(allocation => allocation.DirectShipPriority)
            .ThenByDescending(allocation => allocation.CreatedOnUtc))
        {
            if (quantityToReduce <= 0)
                break;

            var reduceQuantity = Math.Min(allocation.ReceivedQuantity, quantityToReduce);
            allocation.ReduceReceived(reduceQuantity);
            await allocationRepository.UpdateAsync(allocation).ConfigureAwait(false);

            quantityToReduce -= reduceQuantity;
        }
    }
}

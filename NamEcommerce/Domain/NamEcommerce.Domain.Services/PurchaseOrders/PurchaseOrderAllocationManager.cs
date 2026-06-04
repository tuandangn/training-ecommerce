using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Domain.Services.PurchaseOrders;

public sealed class PurchaseOrderAllocationManager(
    IRepository<PurchaseOrderItemAllocation> allocationRepository,
    IEntityDataReader<PurchaseOrderItemAllocation> allocationReader,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IEntityDataReader<PurchaseOrder> purchaseOrderReader,
    IEntityDataReader<Order> orderReader,
    IEntityDataReader<Vendor> vendorReader,
    IEntityDataReader<Product> productReader) : IPurchaseOrderAllocationManager
{
    public async Task<PurchaseOrderItemAllocationDto> AllocatePurchaseOrderItemForOrder(AllocatePurchaseOrderItemForOrder dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var (purchaseOrder, purchaseOrderItem) = EnsurePurchaseOrderItemExists(dto.PurchaseOrderItemId);
        var isValidPurchaseOrderStatus = purchaseOrder.Status
            is PurchaseOrderStatus.Draft
            or PurchaseOrderStatus.Submitted
            or PurchaseOrderStatus.Approved
            or PurchaseOrderStatus.Receiving;
        if (!isValidPurchaseOrderStatus)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemCannotAllocate");

        var (order, orderItem) = EnsureOrderItemExists(dto.OrderItemId);
        EnsureOrderItemCanAllocate(orderItem, purchaseOrderItem.ProductId, dto.AllocationQuantity);

        var allocatedQuantity = (await GetAllocationsOfPurchaseOrderItem(dto.PurchaseOrderItemId, true).ConfigureAwait(false))
            .Sum(allocation => allocation.AllocatedQuantity);
        var availableForAllocation = purchaseOrderItem.QuantityOrdered - allocatedQuantity;
        if (dto.AllocationQuantity > availableForAllocation)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemAllocationQuantityExceedsAvailable");

        var allocation = new PurchaseOrderItemAllocation(dto.PurchaseOrderItemId.SecondaryId, dto.OrderItemId.SecondaryId, dto.AllocationQuantity);
        if (dto.DirectShipInfo is not null)
        {
            var (contactName, contactPhone, address) = dto.DirectShipInfo;
            allocation.SetDirectShip(address ?? string.Empty, contactName, contactPhone, dto.DirectShipInfo.Priority);
        }
        var inserted = await allocationRepository.InsertAsync(allocation).ConfigureAwait(false);

        //temp
        _newPurchaseOrderItemAllocations.Add(inserted);

        return inserted.ToDto();
    }

    public async Task IncreaseReceivedAsync(Guid allocationId, decimal receivedQty)
    {
        var allocation = await allocationRepository.GetByIdAsync(allocationId).ConfigureAwait(false)
            ?? throw new PurchaseOrderItemAllocationIsNotFoundException(allocationId);

        allocation.IncreaseReceived(receivedQty);
        await allocationRepository.UpdateAsync(allocation).ConfigureAwait(false);
    }

    public async Task<DistributeReceivedQuantityResultDto> SyncReceivedForPurchaseOrderItemAsync(Guid purchaseOrderItemId, decimal purchaseOrderItemReceivedQuantity)
    {
        if (purchaseOrderItemId == Guid.Empty)
            throw new PurchaseOrderItemIsNotFoundException();
        if (purchaseOrderItemReceivedQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(purchaseOrderItemReceivedQuantity));

        var allocations = await GetAllocationsOfPurchaseOrderItem((Guid.Empty, purchaseOrderItemId), true).ConfigureAwait(false);
        allocations = allocations.Where(allocation => allocation.Status != AllocationStatus.Cancelled)
                .OrderByDescending(allocation => allocation.IsDirectShip)
                .ThenByDescending(allocation => allocation.DirectShipPriority)
                .ThenBy(allocation => allocation.CreatedOnUtc)
                .ToList();
        if (allocations.Count == 0)
            return EmptyDistributeResult();

        var totalAllocated = allocations.Sum(allocation => allocation.AllocatedQuantity);
        var currentReceived = allocations.Sum(allocation => allocation.ReceivedQuantity);
        var targetReceived = Math.Min(purchaseOrderItemReceivedQuantity, totalAllocated);
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
            var trackedAllocation = await GetTrackedAllocationAsync(allocation.Id).ConfigureAwait(false);
            trackedAllocation.IncreaseReceived(receivedForAllocation);
            await allocationRepository.UpdateAsync(trackedAllocation).ConfigureAwait(false);

            var receipt = new AllocationReceiptDto
            {
                AllocationId = trackedAllocation.Id,
                OrderItemId = trackedAllocation.OrderItemId,
                Quantity = receivedForAllocation,
                IsDirectShip = trackedAllocation.IsDirectShip,
                DirectShipAddress = trackedAllocation.DirectShipAddress
            };
            if (trackedAllocation.IsDirectShip)
                directShipReceipts.Add(receipt);
            else
                warehouseReceipts.Add(receipt);

            quantityToDistribute -= receivedForAllocation;
            if (quantityToDistribute <= 0)
                break;
        }

        if (purchaseOrderItemReceivedQuantity > totalAllocated)
        {
            System.Diagnostics.Trace.TraceWarning(
                $"PurchaseOrderItem {purchaseOrderItemId} received {purchaseOrderItemReceivedQuantity} but only {totalAllocated} was allocated; extra stock remains free.");
        }

        return new DistributeReceivedQuantityResultDto
        {
            DirectShipReceipts = directShipReceipts,
            WarehouseReceipts = warehouseReceipts
        };
    }

    public async Task<IList<PurchaseOrderItemAllocationDto>> GetAllocationsForOrderItemAsync(Guid orderItemId)
    {
        var allocations = await allocationReader.DataSource
            .Where(allocation => allocation.OrderItemId == orderItemId)
            .OrderBy(allocation => allocation.CreatedOnUtc)
            .ToListAsync()
            .ConfigureAwait(false);

        return allocations.Select(allocation => allocation.ToDto()).ToList();
    }

    public async Task<IList<PurchaseOrderItemAllocationDto>> GetAllocationsForPurchaseOrderItemAsync(Guid purchaseOrderItemId)
    {
        var allocations = await GetAllocationsOfPurchaseOrderItem((Guid.Empty, purchaseOrderItemId), true).ConfigureAwait(false);

        return allocations.OrderBy(allocation => allocation.CreatedOnUtc).Select(allocation => allocation.ToDto()).ToList();
    }

    public async Task ReleaseAllocationsOfPurchaseOrderItemAsync(SecondaryItemId purchaseOrderItemId)
    {
        var allocations = await GetAllocationsOfPurchaseOrderItem(purchaseOrderItemId)
            .ConfigureAwait(false);

        await ReleaseAllocations(allocations).ConfigureAwait(false);
    }

    public async Task ReleaseAllocationsForOrderAsync(Guid orderId)
    {
        if (orderId == Guid.Empty)
            return;

        var orderItemIds = await orderReader.DataSource
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

        var allocations = await allocationReader.DataSource
            .Where(allocation => allocation.OrderItemId == orderItemId)
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

    public Task<IList<OrderAllocatedPurchaseOrderDto>> GetAllocatedPurchaseOrdersForOrderAsync(Guid orderId)
    {
        if (orderId == Guid.Empty)
            return Task.FromResult<IList<OrderAllocatedPurchaseOrderDto>>([]);

        var orderItemIds = orderReader.DataSource
            .Where(order => order.Id == orderId)
            .SelectMany(order => order.OrderItems.Select(item => item.Id))
            .ToHashSet();
        if (orderItemIds.Count == 0)
            return Task.FromResult<IList<OrderAllocatedPurchaseOrderDto>>([]);

        var allocations = allocationReader.DataSource
            .Where(allocation => orderItemIds.Contains(allocation.OrderItemId))
            .OrderBy(allocation => allocation.CreatedOnUtc)
            .ToList();
        if (allocations.Count == 0)
            return Task.FromResult<IList<OrderAllocatedPurchaseOrderDto>>([]);

        var purchaseOrderItemIds = allocations.Select(allocation => allocation.PurchaseOrderItemId).ToHashSet();

        var purchaseOrders = purchaseOrderReader.DataSource
            .Where(purchaseOrder => purchaseOrder.Status != PurchaseOrderStatus.Cancelled
                && purchaseOrder.Items.Any(item => purchaseOrderItemIds.Contains(item.Id)))
            .ToList();

        var poItemToPurchaseOrder = purchaseOrders
            .SelectMany(purchaseOrder => purchaseOrder.Items
                .Where(item => purchaseOrderItemIds.Contains(item.Id))
                .Select(item => new { PurchaseOrderItemId = item.Id, PurchaseOrder = purchaseOrder, Item = item }))
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

        return Task.FromResult<IList<OrderAllocatedPurchaseOrderDto>>(result);
    }

    public async Task<IList<EligibleOrderItemForAllocationDto>> GetEligibleOrderItemsForPoItemAsync(Guid purchaseOrderItemId)
    {
        var purchaseOrderItemContext = await purchaseOrderReader.DataSource
            .SelectMany(po => po.Items.Select(item => new { Item = item }))
            .FirstOrDefaultAsync(ctx => ctx.Item.Id == purchaseOrderItemId)
            .ConfigureAwait(false);
        if (purchaseOrderItemContext is null)
            return [];

        var productId = purchaseOrderItemContext.Item.ProductId;

        var eligibleItems = await orderReader.DataSource
            .Where(order => order.OrderStatus != OrderStatus.Cancelled)
            .SelectMany(order => order.OrderItems
                .Where(item => item.ProductId == productId)
                .Select(item => new { Order = order, Item = item }))
            .ToListAsync()
            .ConfigureAwait(false);

        if (eligibleItems.Count == 0)
            return [];

        var eligibleOrderItemIds = eligibleItems.Select(ctx => ctx.Item.Id).ToHashSet();

        var relevantAllocations = await allocationReader.DataSource
            .Where(a => eligibleOrderItemIds.Contains(a.OrderItemId) && a.Status != AllocationStatus.Cancelled)
            .ToListAsync()
            .ConfigureAwait(false);

        var allocatedOutstandingByOrderItemId = relevantAllocations
            .GroupBy(a => a.OrderItemId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(a => Math.Max(0m, a.AllocatedQuantity - a.ReceivedQuantity)));
        var activeDeliveryQuantitiesByOrderItemId = GetActiveDeliveryQuantities(eligibleOrderItemIds);

        return eligibleItems
            .Select(ctx =>
            {
                var outstanding = allocatedOutstandingByOrderItemId.TryGetValue(ctx.Item.Id, out var v) ? v : 0m;
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
                    ShippingAddress = ctx.Order.ShippingAddress
                };
            })
            .Where(dto => dto.AvailableToAllocate > 0)
            .OrderBy(dto => dto.OrderCode)
            .ToList();
    }

    public Task<IList<NonDirectShipAllocationDto>> GetNonDirectShipAllocationsForPoItemAsync(Guid purchaseOrderItemId)
    {
        var allocations = allocationReader.DataSource
            .Where(a => a.PurchaseOrderItemId == purchaseOrderItemId
                && !a.IsDirectShip
                && a.Status != AllocationStatus.Cancelled
                && a.AllocatedQuantity > a.ReceivedQuantity)
            .ToList();
        if (allocations.Count == 0)
            return Task.FromResult<IList<NonDirectShipAllocationDto>>([]);

        var orderItemIds = allocations.Select(a => a.OrderItemId).ToHashSet();
        var orderItems = orderReader.DataSource
            .SelectMany(order => order.OrderItems
                .Where(item => orderItemIds.Contains(item.Id))
                .Select(item => new { Order = order, Item = item }))
            .ToDictionary(x => x.Item.Id);

        var result = allocations
            .Where(a => orderItems.ContainsKey(a.OrderItemId))
            .Select(a =>
            {
                var ctx = orderItems[a.OrderItemId];
                return new NonDirectShipAllocationDto
                {
                    AllocationId = a.Id,
                    OrderId = ctx.Order.Id,
                    OrderItemId = a.OrderItemId,
                    OrderCode = ctx.Order.Code,
                    CustomerName = ctx.Order.CustomerInfo.FullName,
                    CustomerPhone = ctx.Order.CustomerInfo.PhoneNumber,
                    ShippingAddress = ctx.Order.ShippingAddress,
                    AllocatedQuantity = a.AllocatedQuantity,
                    RemainingQuantity = Math.Max(0m, a.AllocatedQuantity - a.ReceivedQuantity)
                };
            })
            .ToList();

        return Task.FromResult<IList<NonDirectShipAllocationDto>>(result);
    }

    private (PurchaseOrder, PurchaseOrderItem) EnsurePurchaseOrderItemExists(SecondaryItemId purchaseOrderItemId)
    {
        var purchaseOrder = purchaseOrderReader.DataSource
            .Where(po => po.Id == purchaseOrderItemId.PrimaryId)
            .FirstOrDefault();
        if (purchaseOrder is null)
            throw new PurchaseOrderItemIsNotFoundException();

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == purchaseOrderItemId.SecondaryId);
        if (purchaseOrderItem is null)
            throw new PurchaseOrderItemIsNotFoundException();

        return (purchaseOrder, purchaseOrderItem);
    }

    private (Order, OrderItem) EnsureOrderItemExists(SecondaryItemId orderItemId)
    {
        var order = orderReader.DataSource.Where(order => order.Id == orderItemId.PrimaryId).FirstOrDefault();
        if (order is null)
            throw new OrderItemIsNotFoundException();

        var orderItem = order.OrderItems.FirstOrDefault(item => item.Id == orderItemId.SecondaryId);
        if (orderItem is null)
            throw new OrderItemIsNotFoundException();

        return (order, orderItem);
    }

    private void EnsureOrderItemCanAllocate(OrderItem orderItem, Guid productId, decimal quantity)
    {
        if (orderItem.ProductId != productId)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemAllocationProductMismatch");

        // Use net outstanding (AllocatedQty - ReceivedQty) so fully/partially received allocations
        // don't permanently block re-allocation when received stock is consumed by other orders.
        // This mirrors ShortageQueryService.allocatedIncoming to keep shortage page and this check consistent.
        var allocatedOutstanding = allocationReader.DataSource
            .Where(allocation => allocation.OrderItemId == orderItem.Id
                && allocation.Status != AllocationStatus.Cancelled)
            .ToList()
            .Sum(allocation => Math.Max(0m, allocation.AllocatedQuantity - allocation.ReceivedQuantity));
        var activeDeliveryQuantity = GetActiveDeliveryQuantity(orderItem.Id);
        var availableQuantity = orderItem.Quantity - activeDeliveryQuantity - allocatedOutstanding;
        if (quantity > availableQuantity)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemAllocationQuantityExceedsAvailable");
    }

    private decimal GetActiveDeliveryQuantity(Guid orderItemId)
        => GetActiveDeliveryQuantities([orderItemId]).GetValueOrDefault(orderItemId);

    private Dictionary<Guid, decimal> GetActiveDeliveryQuantities(IReadOnlyCollection<Guid> orderItemIds)
    {
        if (orderItemIds.Count == 0)
            return [];

        return deliveryNoteReader.DataSource
            .Where(deliveryNote => deliveryNote.Status != DeliveryNoteStatus.Cancelled)
            .SelectMany(deliveryNote => deliveryNote.Items)
            .Where(item => orderItemIds.Contains(item.OrderItemId))
            .GroupBy(item => item.OrderItemId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
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
            var trackedAllocation = await GetTrackedAllocationAsync(allocation.Id).ConfigureAwait(false);
            trackedAllocation.ReduceReceived(reduceQuantity);
            await allocationRepository.UpdateAsync(trackedAllocation).ConfigureAwait(false);

            quantityToReduce -= reduceQuantity;
        }
    }

    private readonly IList<PurchaseOrderItemAllocation> _newPurchaseOrderItemAllocations = new List<PurchaseOrderItemAllocation>();
    private readonly IDictionary<Guid, IList<PurchaseOrderItemAllocation>> _purchaseOrderItemAllocationMap = new Dictionary<Guid, IList<PurchaseOrderItemAllocation>>();
    private async Task<IList<PurchaseOrderItemAllocation>> GetAllocationsOfPurchaseOrderItem(SecondaryItemId purchaseOrderItemId, bool ignoreUpdateCache = false)
    {
        var matchedNewAllocations = _newPurchaseOrderItemAllocations.Where(allocation => allocation.PurchaseOrderItemId == purchaseOrderItemId.SecondaryId).ToList();

        if (!ignoreUpdateCache && _purchaseOrderItemAllocationMap.TryGetValue(purchaseOrderItemId.SecondaryId, out var allocations))
            return returnResult(allocations);

        allocations = await allocationReader.DataSource
                .Where(allocation => allocation.PurchaseOrderItemId == purchaseOrderItemId.SecondaryId)
                .ToListAsync().ConfigureAwait(false);
        if (!ignoreUpdateCache && allocations.Any())
            _purchaseOrderItemAllocationMap.Add(purchaseOrderItemId.SecondaryId, allocations);
        return returnResult(allocations);

        //local method
        IList<PurchaseOrderItemAllocation> returnResult(IEnumerable<PurchaseOrderItemAllocation> items)
            => items.Where(allocation => !matchedNewAllocations.Any(newAllocation => newAllocation.Id == allocation.Id))
                .Concat(matchedNewAllocations).ToList();
    }

    private async Task<PurchaseOrderItemAllocation> GetTrackedAllocationAsync(Guid allocationId)
        => await allocationRepository.GetByIdAsync(allocationId).ConfigureAwait(false)
            ?? throw new PurchaseOrderItemAllocationIsNotFoundException(allocationId);
}

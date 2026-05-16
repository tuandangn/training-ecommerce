using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Domain.Services.PurchaseOrders;

public sealed class PurchaseOrderAllocationManager(
    IRepository<PurchaseOrderItemAllocation> allocationRepository,
    IEntityDataReader<PurchaseOrderItemAllocation> allocationReader,
    IEntityDataReader<PurchaseOrder> purchaseOrderReader,
    IEntityDataReader<Order> orderReader,
    IEntityDataReader<Vendor> vendorReader,
    IEntityDataReader<Product> productReader) : IPurchaseOrderAllocationManager
{
    public async Task<PurchaseOrderItemAllocationDto> AllocateAsync(Guid purchaseOrderItemId, Guid orderItemId, decimal quantity)
    {
        if (quantity <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.AllocatedQuantityMustBePositive");

        EnsurePurchaseOrderItemExists(purchaseOrderItemId);
        EnsureOrderItemExists(orderItemId);

        var allocation = new PurchaseOrderItemAllocation(purchaseOrderItemId, orderItemId, quantity);
        var inserted = await allocationRepository.InsertAsync(allocation).ConfigureAwait(false);

        return inserted.ToDto();
    }

    public async Task<PurchaseOrderItemAllocationDto> AllocateFromExistingPurchaseOrderItemAsync(Guid purchaseOrderItemId, Guid orderItemId, decimal quantity)
    {
        if (quantity <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.AllocatedQuantityMustBePositive");

        var purchaseOrderItemContext = purchaseOrderReader.DataSource
            .SelectMany(purchaseOrder => purchaseOrder.Items.Select(item => new { PurchaseOrder = purchaseOrder, Item = item }))
            .FirstOrDefault(context => context.Item.Id == purchaseOrderItemId);
        if (purchaseOrderItemContext is null)
            throw new PurchaseOrderItemIsNotFoundException(purchaseOrderItemId);

        var canAllocate = purchaseOrderItemContext.PurchaseOrder.Status is PurchaseOrderStatus.Draft
            or PurchaseOrderStatus.Submitted
            or PurchaseOrderStatus.Approved;
        if (!canAllocate)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemCannotAllocate");

        var orderItem = orderReader.DataSource
            .SelectMany(order => order.OrderItems)
            .FirstOrDefault(item => item.Id == orderItemId);
        if (orderItem is null)
            throw new OrderItemIsNotFoundException(orderItemId);
        if (orderItem.ProductId != purchaseOrderItemContext.Item.ProductId)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemAllocationProductMismatch");

        var allocatedQuantity = allocationReader.DataSource
            .Where(allocation => allocation.PurchaseOrderItemId == purchaseOrderItemId)
            .Sum(allocation => allocation.AllocatedQuantity);
        var availableForAllocation = purchaseOrderItemContext.Item.QuantityOrdered - allocatedQuantity;
        if (quantity > availableForAllocation)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemAllocationQuantityExceedsAvailable");

        var allocation = new PurchaseOrderItemAllocation(purchaseOrderItemId, orderItemId, quantity);
        var inserted = await allocationRepository.InsertAsync(allocation).ConfigureAwait(false);

        return inserted.ToDto();
    }

    public async Task IncreaseReceivedAsync(Guid allocationId, decimal receivedQty)
    {
        var allocation = await allocationRepository.GetByIdAsync(allocationId).ConfigureAwait(false)
            ?? throw new PurchaseOrderItemAllocationIsNotFoundException(allocationId);

        allocation.IncreaseReceived(receivedQty);
        await allocationRepository.UpdateAsync(allocation).ConfigureAwait(false);
    }

    public async Task SyncReceivedForPurchaseOrderItemAsync(Guid purchaseOrderItemId, decimal purchaseOrderItemReceivedQuantity)
    {
        if (purchaseOrderItemId == Guid.Empty)
            throw new PurchaseOrderItemIsNotFoundException(purchaseOrderItemId);
        if (purchaseOrderItemReceivedQuantity <= 0)
            return;

        var allocations = allocationReader.DataSource
            .Where(allocation => allocation.PurchaseOrderItemId == purchaseOrderItemId)
            .OrderBy(allocation => allocation.CreatedOnUtc)
            .ToList();
        if (allocations.Count == 0)
            return;

        var totalAllocated = allocations.Sum(allocation => allocation.AllocatedQuantity);
        var currentReceived = allocations.Sum(allocation => allocation.ReceivedQuantity);
        var targetReceived = Math.Min(purchaseOrderItemReceivedQuantity, totalAllocated);
        var quantityToDistribute = targetReceived - currentReceived;
        if (quantityToDistribute <= 0)
            return;

        foreach (var allocation in allocations)
        {
            var remainingAllocation = allocation.AllocatedQuantity - allocation.ReceivedQuantity;
            if (remainingAllocation <= 0)
                continue;

            var receivedForAllocation = Math.Min(remainingAllocation, quantityToDistribute);
            await IncreaseReceivedAsync(allocation.Id, receivedForAllocation).ConfigureAwait(false);

            quantityToDistribute -= receivedForAllocation;
            if (quantityToDistribute <= 0)
                break;
        }

        if (purchaseOrderItemReceivedQuantity > totalAllocated)
        {
            System.Diagnostics.Trace.TraceWarning(
                $"PurchaseOrderItem {purchaseOrderItemId} received {purchaseOrderItemReceivedQuantity} but only {totalAllocated} was allocated; extra stock remains free.");
        }
    }

    public Task<IList<PurchaseOrderItemAllocationDto>> GetAllocationsForOrderItemAsync(Guid orderItemId)
    {
        var allocations = allocationReader.DataSource
            .Where(allocation => allocation.OrderItemId == orderItemId)
            .OrderBy(allocation => allocation.CreatedOnUtc)
            .ToList()
            .Select(allocation => allocation.ToDto())
            .ToList();

        return Task.FromResult<IList<PurchaseOrderItemAllocationDto>>(allocations);
    }

    public Task<IList<PurchaseOrderItemAllocationDto>> GetAllocationsForPurchaseOrderItemAsync(Guid purchaseOrderItemId)
    {
        var allocations = allocationReader.DataSource
            .Where(allocation => allocation.PurchaseOrderItemId == purchaseOrderItemId)
            .OrderBy(allocation => allocation.CreatedOnUtc)
            .ToList()
            .Select(allocation => allocation.ToDto())
            .ToList();

        return Task.FromResult<IList<PurchaseOrderItemAllocationDto>>(allocations);
    }

    public async Task ReleaseAllocationsForOrderItemAsync(Guid orderItemId)
    {
        if (orderItemId == Guid.Empty)
            return;

        var allocations = allocationReader.DataSource
            .Where(allocation => allocation.OrderItemId == orderItemId)
            .ToList();

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

    public async Task ReleaseAllocationsForOrderAsync(Guid orderId)
    {
        if (orderId == Guid.Empty)
            return;

        var orderItemIds = orderReader.DataSource
            .Where(order => order.Id == orderId)
            .SelectMany(order => order.OrderItems.Select(item => item.Id))
            .ToList();

        foreach (var orderItemId in orderItemIds)
            await ReleaseAllocationsForOrderItemAsync(orderItemId).ConfigureAwait(false);
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

    private void EnsurePurchaseOrderItemExists(Guid purchaseOrderItemId)
    {
        var exists = purchaseOrderReader.DataSource.Any(purchaseOrder => purchaseOrder.Items.Any(item => item.Id == purchaseOrderItemId));
        if (!exists)
            throw new PurchaseOrderItemIsNotFoundException(purchaseOrderItemId);
    }

    private void EnsureOrderItemExists(Guid orderItemId)
    {
        var exists = orderReader.DataSource.Any(order => order.OrderItems.Any(item => item.Id == orderItemId));
        if (!exists)
            throw new OrderItemIsNotFoundException(orderItemId);
    }
}

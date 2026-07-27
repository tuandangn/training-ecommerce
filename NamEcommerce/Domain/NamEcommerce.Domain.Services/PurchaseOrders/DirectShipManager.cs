using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Domain.Services.PurchaseOrders;

public sealed class DirectShipManager(
    IRepository<PurchaseOrderItemAllocation> allocationRepository,
    IEntityDataReader<PurchaseOrderItemAllocation> allocationReader,
    IRepository<DirectShipAddressChangeLog> changeLogRepository,
    IRepository<DeliveryNote> deliveryNoteRepository,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IRepository<PurchaseOrder> purchaseOrderRepository,
    IEntityDataReader<Order> orderReader,
    IEntityDataReader<Warehouse> warehouseReader,
    IDeliveryNoteManager deliveryNoteManager,
    IInventoryStockManager inventoryStockManager,
    IInventoryCostingManager inventoryCostingManager) : IDirectShipManager
{
    public async Task MarkAllocationAsDirectShipAsync(Guid allocationId, string address, string? contactName, string? contactPhone, int priority)
    {
        var allocation = await allocationRepository.GetByIdAsync(allocationId)
            ?? throw new PurchaseOrderItemAllocationIsNotFoundException(allocationId);
        if (allocation.ReceivedQuantity >= allocation.AllocatedQuantity)
            throw new PurchaseOrderItemDataIsInvalidException("Error.DirectShipAllocationNoRemainingQuantity");

        allocation.SetDirectShip(address, contactName, contactPhone, priority);
        await allocationRepository.UpdateAsync(allocation).ConfigureAwait(false);
    }

    public async Task<bool> HasReceivableDirectShipAllocationsAsync(Guid purchaseOrderItemId)
    {
        return await allocationReader.DataSource.AnyAsync(allocation =>
            allocation.PurchaseOrderItemId.SecondaryId == purchaseOrderItemId
           && allocation.IsDirectShip
           && allocation.Status != AllocationStatus.Cancelled
           && allocation.ReceivedQuantity < allocation.AllocatedQuantity).ConfigureAwait(false);
    }

    public async Task<bool> HasReceivedDirectShipAllocationsAsync(Guid orderId)
    {
        if (orderId == Guid.Empty)
            return false;

        var orderItemIds = await orderReader.DataSource
            .Where(o => o.Id == orderId)
            .SelectMany(o => o.OrderItems.Select(i => i.Id))
            .Distinct()
            .ToListAsync().ConfigureAwait(false);

        if (orderItemIds.Count == 0)
            return false;

        var result = await allocationReader.DataSource
            .AnyAsync(allocation => allocation.IsDirectShip
                && allocation.ReceivedQuantity > 0
                && orderItemIds.Contains(allocation.OrderItemId.SecondaryId)).ConfigureAwait(false);
        return result;
    }

    public async Task OnAllocationReceivedAsync(Guid allocationId, decimal receivedDelta, Guid sourceGoodsReceiptId, Guid receivedWarehouseId)
    {
        if (receivedDelta <= 0)
            return;

        var allocation = await allocationRepository.GetByIdAsync(allocationId)
            ?? throw new PurchaseOrderItemAllocationIsNotFoundException(allocationId);
        if (!allocation.IsDirectShip)
            return;

        var purchaseOrderItem = await ResolvePurchaseOrderItem(allocation.PurchaseOrderItemId).ConfigureAwait(false);
        var transitWarehouse = await GetTransitWarehouse().ConfigureAwait(false);

        if (transitWarehouse.Id != receivedWarehouseId)
        {
            await TransferStockWithCostAsync(
                purchaseOrderItem.ProductId,
                receivedWarehouseId,
                transitWarehouse.Id,
                receivedDelta,
                sourceGoodsReceiptId,
                allocation.Id,
                Guid.Empty,
                $"Chuyển hàng giao thẳng từ phiếu nhập {sourceGoodsReceiptId}").ConfigureAwait(false);
        }

        await deliveryNoteManager.CreateForDirectShipAsync(
            new CreateDeliveryNoteForDirectShipDto
            {
                GoodsReceiptId = sourceGoodsReceiptId,
                OrderId = allocation.OrderItemId.PrimaryId,
                OrderItemId = allocation.OrderItemId.SecondaryId,
                Quantity = receivedDelta,
                DirectShipWarehouseId = transitWarehouse.Id,
                ShippingAddress = allocation.DirectShipAddress ?? string.Empty,
                ContactName = allocation.DirectShipContactName,
                ContactPhone = allocation.DirectShipContactPhone
            }).ConfigureAwait(false);
    }

    public async Task ConfirmDeliveryAsync(Guid deliveryNoteId, DateTime confirmedAtUtc, string? note)
    {
        await deliveryNoteManager.ConfirmDirectShipDeliveryAsync(deliveryNoteId, confirmedAtUtc, note).ConfigureAwait(false);
    }

    public async Task RejectDeliveryAsync(Guid deliveryNoteId, Guid returnWarehouseId, string reason)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(deliveryNoteId)
            ?? throw new DeliveryNoteNotFoundException(deliveryNoteId);

        EnsureCanRejectDirectShipDelivery(deliveryNote);
        await ReturnDirectShipStockAsync(deliveryNote, returnWarehouseId, reason).ConfigureAwait(false);
        await deliveryNoteManager.RejectDirectShipDeliveryAsync(deliveryNoteId, reason)
            .ConfigureAwait(false);
    }

    public async Task HandleSoCancelledForReceivedDirectShipAsync(Guid orderId, Guid returnWarehouseId, Guid userId, string? reason)
    {
        var deliveryNoteIds = await deliveryNoteReader.DataSource
            .Where(d => d.OrderId == orderId
                && d.SourceType == DeliveryNoteSourceType.DirectShipToCustomer
                && d.Status == DeliveryNoteStatus.Confirmed)
            .Select(d => d.Id)
            .ToListAsync().ConfigureAwait(false);

        foreach (var deliveryNoteId in deliveryNoteIds)
        {
            var deliveryNote = await deliveryNoteRepository.GetByIdAsync(deliveryNoteId)
                ?? throw new DeliveryNoteNotFoundException(deliveryNoteId);

            var note = string.IsNullOrWhiteSpace(reason)
                ? $"Đơn bán {orderId} bị hủy — chuyển hàng giao thẳng về kho đã chọn"
                : reason;
            EnsureCanRejectDirectShipDelivery(deliveryNote);
            await ReturnDirectShipStockAsync(deliveryNote, returnWarehouseId, note, userId).ConfigureAwait(false);
            await deliveryNoteManager.RejectDirectShipDeliveryAsync(deliveryNoteId, note)
                .ConfigureAwait(false);
        }
    }

    public async Task UpdateDirectShipAddressAsync(Guid allocationId,
        string newAddress, string? newContactName, string? newContactPhone,
        Guid editedByUserId, string? reason)
    {
        var allocation = await allocationRepository.GetByIdAsync(allocationId)
            ?? throw new PurchaseOrderItemAllocationIsNotFoundException(allocationId);

        var changeLog = new DirectShipAddressChangeLog(
            allocationId,
            allocation.DirectShipAddress ?? string.Empty,
            newAddress,
            allocation.DirectShipContactName, newContactName,
            allocation.DirectShipContactPhone, newContactPhone,
            editedByUserId,
            reason);

        allocation.UpdateDirectShipInfo(newAddress, newContactName, newContactPhone, editedByUserId);

        await allocationRepository.UpdateAsync(allocation).ConfigureAwait(false);
        await changeLogRepository.InsertAsync(changeLog).ConfigureAwait(false);
    }

    public async Task<IList<DeliveryNoteDto>> GetPendingDeliveriesAsync(string? keywords, DateTime? fromDateUtc, DateTime? toDateUtc)
    {
        var query = deliveryNoteReader.DataSource
            .Where(d => d.SourceType == DeliveryNoteSourceType.DirectShipToCustomer && d.Status == DeliveryNoteStatus.Confirmed);

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            var kw = keywords.Trim().ToLower();
            query = query.Where(d =>
                d.Code.ToLower().Contains(kw) ||
                d.CustomerInfo.FullName.Value.ToLower().Contains(kw) ||
                d.ShippingAddress.Value.ToLower().Contains(kw));
        }

        if (fromDateUtc.HasValue)
            query = query.Where(d => d.CreatedOnUtc >= fromDateUtc.Value);

        if (toDateUtc.HasValue)
            query = query.Where(d => d.CreatedOnUtc <= toDateUtc.Value);

        var results = await query
            .OrderByDescending(d => d.CreatedOnUtc)
            .ToListAsync().ConfigureAwait(false);

        var dtos = results.Select(MapDeliveryNote).ToList();

        return dtos;
    }

    public async Task<IList<DirectShipAllocationStatusDto>> GetDirectShipAllocationsForOrderItemsAsync(IReadOnlyList<SecondaryItemId> orderItemIds)
    {
        var deliveryNotesByOrderItems = await GetDirectShipDeliveryNotesByOrderItems(orderItemIds).ConfigureAwait(false);

        var itemIds = orderItemIds.Select(id => id.SecondaryId).Distinct().ToList();
        var allocations = await allocationReader.DataSource
            .Where(a => a.IsDirectShip && itemIds.Contains(a.OrderItemId.SecondaryId))
            .ToListAsync().ConfigureAwait(false);

        var results = allocations.Select(allocation =>
        {
            deliveryNotesByOrderItems.TryGetValue(allocation.OrderItemId.SecondaryId, out var deliveryNote);
            return new DirectShipAllocationStatusDto
            {
                AllocationId = allocation.Id,
                OrderId = allocation.OrderItemId.PrimaryId,
                OrderItemId = allocation.OrderItemId.SecondaryId,
                Status = (int)allocation.Status,
                DeliveryStatus = deliveryNote is null ? null : (int)deliveryNote.Status,
                DeliveryNoteId = deliveryNote?.Id,
                DeliveryNoteCode = deliveryNote?.Code,
                AllocatedQuantity = allocation.AllocatedQuantity,
                ReceivedQuantity = allocation.ReceivedQuantity
            };
        }).ToList();

        return results;
    }

    public async Task<IList<DirectShipAllocationForPoItemDto>> GetDirectShipAllocationsForPoItemsAsync(IReadOnlyList<SecondaryItemId> purchaseOrderItemIds)
    {
        var poItemIds = purchaseOrderItemIds.Select(id => id.SecondaryId).Distinct().ToList();
        var orderItemIds = await allocationReader.DataSource
            .Where(allocation => allocation.IsDirectShip && poItemIds.Contains(allocation.PurchaseOrderItemId.SecondaryId))
            .Select(allocation => allocation.OrderItemId)
            .Distinct()
            .ToListAsync().ConfigureAwait(false);
        var deliveryNotesByOrderItems = await GetDirectShipDeliveryNotesByOrderItems(orderItemIds).ConfigureAwait(false);

        var allocations = await allocationReader.DataSource
            .Where(a => a.IsDirectShip && poItemIds.Contains(a.PurchaseOrderItemId.SecondaryId))
            .ToListAsync().ConfigureAwait(false);
        var results = allocations.Select(allocation =>
        {
            deliveryNotesByOrderItems.TryGetValue(allocation.OrderItemId.SecondaryId, out var deliveryNote);
            return new DirectShipAllocationForPoItemDto
            {
                AllocationId = allocation.Id,
                PurchaseOrderId = allocation.PurchaseOrderItemId.PrimaryId,
                PurchaseOrderItemId = allocation.PurchaseOrderItemId.SecondaryId,
                DirectShipAddress = allocation.DirectShipAddress ?? string.Empty,
                DirectShipContactName = allocation.DirectShipContactName,
                DirectShipContactPhone = allocation.DirectShipContactPhone,
                AllocatedQuantity = allocation.AllocatedQuantity,
                ReceivedQuantity = allocation.ReceivedQuantity,
                Status = (int)allocation.Status,
                DeliveryStatus = deliveryNote is null ? null : (int)deliveryNote.Status,
                DeliveryNoteId = deliveryNote?.Id,
                DeliveryNoteCode = deliveryNote?.Code
            };
        }).ToList();

        return results;
    }

    private async Task<PurchaseOrderItem> ResolvePurchaseOrderItem(SecondaryItemId purchaseOrderItemId)
    {
        var purchaseOrder = await purchaseOrderRepository.GetByIdAsync(purchaseOrderItemId.PrimaryId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderItemId.PrimaryId);

        var purchaseOrderItem = purchaseOrder.Items
            .FirstOrDefault(item => item.Id == purchaseOrderItemId.SecondaryId);

        if (purchaseOrderItem is null)
            throw new PurchaseOrderItemIsNotFoundException();

        return purchaseOrderItem;
    }

    private async Task<Warehouse> GetTransitWarehouse()
    {
        var transitWarehouse = await warehouseReader.DataSource
            .FirstOrDefaultAsync(w => w.WarehouseType == WarehouseType.DirectTransit).ConfigureAwait(false);

        if (transitWarehouse is null)
            throw new DirectShipTransitWarehouseNotConfiguredException();

        return transitWarehouse;
    }

    public async Task<Guid> GetTransitWarehouseIdAsync()
    {
        var transitWarehouse = await GetTransitWarehouse().ConfigureAwait(false);
        return transitWarehouse.Id;
    }

    private static void EnsureCanRejectDirectShipDelivery(DeliveryNote deliveryNote)
    {
        if (!deliveryNote.IsDirectShip || deliveryNote.SourceType != DeliveryNoteSourceType.DirectShipToCustomer)
            throw new DeliveryNoteCannotChangeStatusException(deliveryNote.Status, deliveryNote.Status);
        if (deliveryNote.Status != DeliveryNoteStatus.Confirmed)
            throw new DeliveryNoteCannotChangeStatusException(deliveryNote.Status, DeliveryNoteStatus.Cancelled);
    }

    private async Task ReturnDirectShipStockAsync(DeliveryNote deliveryNote, Guid returnWarehouseId, string reason, Guid userId = default)
    {
        var returnWarehouse = await ResolveReturnWarehouseAsync(returnWarehouseId).ConfigureAwait(false);
        foreach (var item in deliveryNote.Items)
        {
            var warehouseId = item.WarehouseId;
            if (warehouseId == Guid.Empty)
                throw new WarehouseIsNotSuitableException(Guid.Empty);
            await inventoryStockManager.ReleaseReservedStockAsync(item.ProductId, warehouseId, item.Quantity, deliveryNote.Id, userId);
            await TransferStockWithCostAsync(
                item.ProductId,
                warehouseId,
                returnWarehouse.Id,
                item.Quantity,
                deliveryNote.Id,
                item.Id,
                userId,
                reason).ConfigureAwait(false);
        }
    }

    private async Task<Warehouse> ResolveReturnWarehouseAsync(Guid returnWarehouseId)
    {
        var warehouse = await warehouseReader.GetByIdAsync(returnWarehouseId)
            ?? throw new WarehouseIsNotFoundException(returnWarehouseId);

        if (!warehouse.IsActive || warehouse.WarehouseType != WarehouseType.Physical)
            throw new WarehouseIsNotSuitableException(returnWarehouseId);

        return warehouse;
    }

    private async Task TransferStockWithCostAsync(Guid productId,
        Guid fromWarehouseId, Guid toWarehouseId, decimal quantity,
        Guid referenceId, Guid referenceItemId, Guid userId, string reason)
    {
        if (fromWarehouseId == toWarehouseId)
            return;

        var costSummary = await inventoryCostingManager.GetCurrentCostSummaryAsync(productId).ConfigureAwait(false);

        await inventoryStockManager.TransferStockAsync(
            productId,
            fromWarehouseId,
            toWarehouseId,
            quantity,
            costSummary.AverageCost,
            referenceId,
            userId,
            reason).ConfigureAwait(false);

        var occurredAtUtc = DateTime.UtcNow;
        var transferOutCost = await inventoryCostingManager.RegisterOutboundAsync(new RegisterInventoryOutboundCostDto
        {
            ProductId = productId,
            WarehouseId = fromWarehouseId,
            Quantity = quantity,
            MovementType = InventoryCostMovementType.TransferOut,
            ReferenceType = InventoryCostReferenceType.StockTransfer,
            ReferenceId = referenceId,
            ReferenceItemId = referenceItemId,
            OccurredAtUtc = occurredAtUtc
        }).ConfigureAwait(false);

        await inventoryCostingManager.RegisterTransferInAsync(new RegisterInventoryTransferInCostDto
        {
            ProductId = productId,
            WarehouseId = toWarehouseId,
            Quantity = quantity,
            UnitCost = transferOutCost.UnitCost,
            SourceStatus = transferOutCost.Status,
            ReferenceType = InventoryCostReferenceType.StockTransfer,
            ReferenceId = referenceId,
            ReferenceItemId = referenceItemId,
            OccurredAtUtc = occurredAtUtc
        }).ConfigureAwait(false);
    }

    private async Task<IDictionary<Guid, DeliveryNote>> GetDirectShipDeliveryNotesByOrderItems(IEnumerable<SecondaryItemId> orderItemIds)
    {
        var orderIds = orderItemIds.Select(id => id.PrimaryId).Distinct().ToList();
        var itemIds = orderItemIds.Select(id => id.SecondaryId).Distinct().ToList();
        return await deliveryNoteReader.DataSource
                .Where(deliveryNote => deliveryNote.SourceType == DeliveryNoteSourceType.DirectShipToCustomer
                    && orderIds.Contains(deliveryNote.OrderId))
                .SelectMany(deliveryNote => deliveryNote.Items.Select(item => new { item.OrderItemId, DeliveryNote = deliveryNote }))
                .Where(x => itemIds.Contains(x.OrderItemId))
                .GroupBy(x => x.OrderItemId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.DeliveryNote.CreatedOnUtc).First().DeliveryNote)
                .ConfigureAwait(false);
    }

    private static DeliveryNoteDto MapDeliveryNote(DeliveryNote d)
        => new()
        {
            Id = d.Id,
            Code = d.Code,
            OrderId = d.OrderId,
            OrderCode = d.OrderCode,
            CustomerId = d.CustomerId,
            CustomerName = d.CustomerInfo.FullName,
            CustomerPhone = d.CustomerInfo.PhoneNumber,
            CustomerAddress = d.CustomerInfo.Address,
            ShippingAddress = d.ShippingAddress,
            ShippingPhoneNumber = d.ShippingPhoneNumber,
            ShowPrice = d.ShowPrice,
            Note = d.Note,
            Status = d.Status,
            SourceType = d.SourceType,
            IsDirectShip = d.IsDirectShip,
            DeliveryConfirmationStatus = d.DeliveryConfirmationStatus,
            CreatedOnUtc = d.CreatedOnUtc,
            DeliveredOnUtc = d.DeliveredOnUtc,
            DeliveryProofPictureId = d.DeliveryProofPictureId,
            DeliveryReceiverName = d.DeliveryReceiverName,
            DeliveryCashCollectedAmount = d.DeliveryCashCollectedAmount,
            TotalAmount = d.TotalAmount,
            Surcharge = d.Surcharge,
            SurchargeReason = d.SurchargeReason,
            AmountToCollect = d.AmountToCollect,
            CreatedByUserId = d.CreatedByUserId,
            UpdatedOnUtc = d.UpdatedOnUtc,
            Items = d.Items.Select(i => new DeliveryNoteItemDto
            {
                Id = i.Id,
                DeliveryNoteId = i.DeliveryNoteId,
                OrderItemId = i.OrderItemId,
                ProductId = i.ProductId,
                WarehouseId = i.WarehouseId,
                ProductName = i.ProductName ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal,
                CostAtDispatch = i.CostAtDispatch
            }).ToList()
        };
}

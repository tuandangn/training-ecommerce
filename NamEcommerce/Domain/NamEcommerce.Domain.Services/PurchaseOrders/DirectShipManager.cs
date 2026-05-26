using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.GoodsReceipts;
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
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IEntityDataReader<PurchaseOrder> purchaseOrderReader,
    IEntityDataReader<Order> orderReader,
    IEntityDataReader<Warehouse> warehouseReader,
    IDeliveryNoteManager deliveryNoteManager,
    IInventoryStockManager stockManager,
    IInventoryCostingManager inventoryCostingManager) : IDirectShipManager
{
    public async Task MarkAllocationAsDirectShipAsync(
        Guid allocationId, string address, string? contactName, string? contactPhone, int priority,
        CancellationToken ct = default)
    {
        var allocation = await allocationReader.GetByIdAsync(allocationId)
            ?? throw new PurchaseOrderItemAllocationIsNotFoundException(allocationId);

        allocation.SetDirectShip(address, contactName, contactPhone, priority);
        await allocationRepository.UpdateAsync(allocation, ct).ConfigureAwait(false);
    }

    public Task<bool> HasReceivableDirectShipAllocationsAsync(
        Guid purchaseOrderItemId, CancellationToken ct = default)
    {
        var result = allocationReader.DataSource
            .Any(a => a.PurchaseOrderItemId == purchaseOrderItemId
                   && a.IsDirectShip
                   && a.Status != AllocationStatus.Cancelled
                   && a.ReceivedQuantity < a.AllocatedQuantity);
        return Task.FromResult(result);
    }

    public Task<bool> HasReceivedDirectShipAllocationsAsync(
        Guid orderId, CancellationToken ct = default)
    {
        if (orderId == Guid.Empty)
            return Task.FromResult(false);

        var orderItemIds = orderReader.DataSource
            .Where(o => o.Id == orderId)
            .SelectMany(o => o.OrderItems.Select(i => i.Id))
            .Distinct()
            .ToList();

        if (orderItemIds.Count == 0)
            return Task.FromResult(false);

        var result = allocationReader.DataSource
            .Any(a => a.IsDirectShip
                && a.ReceivedQuantity > 0
                && orderItemIds.Contains(a.OrderItemId));
        return Task.FromResult(result);
    }

    public async Task OnAllocationReceivedAsync(
        Guid allocationId,
        decimal receivedDelta,
        Guid sourceGoodsReceiptId,
        Guid receivedWarehouseId,
        CancellationToken ct = default)
    {
        if (receivedDelta <= 0)
            return;

        var allocation = await allocationReader.GetByIdAsync(allocationId)
            ?? throw new PurchaseOrderItemAllocationIsNotFoundException(allocationId);
        if (!allocation.IsDirectShip)
            return;

        var purchaseOrderItem = ResolvePurchaseOrderItem(allocation.PurchaseOrderItemId);
        var transitWarehouse = GetTransitWarehouse();

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
                OrderItemId = allocation.OrderItemId,
                Quantity = receivedDelta,
                DirectShipWarehouseId = transitWarehouse.Id,
                ShippingAddress = allocation.DirectShipAddress ?? string.Empty,
                ContactName = allocation.DirectShipContactName,
                ContactPhone = allocation.DirectShipContactPhone
            }, ct).ConfigureAwait(false);
    }

    public async Task ConfirmDeliveryAsync(Guid deliveryNoteId, DateTime confirmedAtUtc, string? note, CancellationToken ct = default)
    {
        await deliveryNoteManager.ConfirmDirectShipDeliveryAsync(
            deliveryNoteId,
            confirmedAtUtc,
            note,
            ct).ConfigureAwait(false);
    }

    public async Task RejectDeliveryAsync(Guid deliveryNoteId, Guid returnWarehouseId, string reason, CancellationToken ct = default)
    {
        var deliveryNote = await deliveryNoteReader.GetByIdAsync(deliveryNoteId)
            ?? throw new DeliveryNoteNotFoundException(deliveryNoteId);

        EnsureCanRejectDirectShipDelivery(deliveryNote);
        await ReturnDirectShipStockAsync(deliveryNote, returnWarehouseId, reason).ConfigureAwait(false);
        await deliveryNoteManager.RejectDirectShipDeliveryAsync(deliveryNoteId, reason, ct)
            .ConfigureAwait(false);
    }

    public async Task HandleSoCancelledForReceivedDirectShipAsync(
        Guid orderId,
        Guid returnWarehouseId,
        Guid userId,
        string? reason,
        CancellationToken ct = default)
    {
        var deliveryNoteIds = deliveryNoteReader.DataSource
            .Where(d => d.OrderId == orderId
                && d.SourceType == DeliveryNoteSourceType.DirectShipToCustomer
                && d.Status == DeliveryNoteStatus.Confirmed)
            .Select(d => d.Id)
            .ToList();

        foreach (var deliveryNoteId in deliveryNoteIds)
        {
            var deliveryNote = await deliveryNoteReader.GetByIdAsync(deliveryNoteId)
                ?? throw new DeliveryNoteNotFoundException(deliveryNoteId);

            var note = string.IsNullOrWhiteSpace(reason)
                ? $"Đơn bán {orderId} bị hủy — chuyển hàng giao thẳng về kho đã chọn"
                : reason;
            EnsureCanRejectDirectShipDelivery(deliveryNote);
            await ReturnDirectShipStockAsync(deliveryNote, returnWarehouseId, note, userId).ConfigureAwait(false);
            await deliveryNoteManager.RejectDirectShipDeliveryAsync(deliveryNoteId, note, ct)
                .ConfigureAwait(false);
        }
    }

    public async Task UpdateDirectShipAddressAsync(
        Guid allocationId, string newAddress, string? newContactName, string? newContactPhone,
        Guid editedByUserId, string? reason, CancellationToken ct = default)
    {
        var allocation = await allocationReader.GetByIdAsync(allocationId)
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

        await allocationRepository.UpdateAsync(allocation, ct).ConfigureAwait(false);
        await changeLogRepository.InsertAsync(changeLog, ct).ConfigureAwait(false);
    }

    public Task<IList<DeliveryNoteDto>> GetPendingDeliveriesAsync(
        string? keywords, DateTime? fromDateUtc, DateTime? toDateUtc, CancellationToken ct = default)
    {
        var query = deliveryNoteReader.DataSource
            .Where(d => d.SourceType == DeliveryNoteSourceType.DirectShipToCustomer
                     && d.Status == DeliveryNoteStatus.Confirmed);

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

        var results = query
            .OrderByDescending(d => d.CreatedOnUtc)
            .ToList();

        IList<DeliveryNoteDto> dtos = results.Select(MapDeliveryNote).ToList();

        return Task.FromResult(dtos);
    }

    public Task<IList<DirectShipAllocationStatusDto>> GetDirectShipAllocationsForOrderItemsAsync(
        IReadOnlyList<Guid> orderItemIds, CancellationToken ct = default)
    {
        var deliveryNotesByOrderItem = GetDirectShipDeliveryNotesByOrderItem(orderItemIds);

        IList<DirectShipAllocationStatusDto> results = allocationReader.DataSource
            .Where(a => a.IsDirectShip && orderItemIds.Contains(a.OrderItemId))
            .ToList()
            .Select(a =>
            {
                deliveryNotesByOrderItem.TryGetValue(a.OrderItemId, out var deliveryNote);
                return new DirectShipAllocationStatusDto
                {
                    AllocationId = a.Id,
                    OrderItemId = a.OrderItemId,
                    Status = (int)a.Status,
                    DeliveryStatus = deliveryNote is null ? null : (int)deliveryNote.Status,
                    DeliveryNoteId = deliveryNote?.Id,
                    DeliveryNoteCode = deliveryNote?.Code,
                    AllocatedQuantity = a.AllocatedQuantity,
                    ReceivedQuantity = a.ReceivedQuantity
                };
            })
            .ToList();
        return Task.FromResult(results);
    }

    public Task<IList<DirectShipAllocationForPoItemDto>> GetDirectShipAllocationsForPoItemsAsync(
        IReadOnlyList<Guid> purchaseOrderItemIds, CancellationToken ct = default)
    {
        var orderItemIds = allocationReader.DataSource
            .Where(a => a.IsDirectShip && purchaseOrderItemIds.Contains(a.PurchaseOrderItemId))
            .Select(a => a.OrderItemId)
            .Distinct()
            .ToList();
        var deliveryNotesByOrderItem = GetDirectShipDeliveryNotesByOrderItem(orderItemIds);

        IList<DirectShipAllocationForPoItemDto> results = allocationReader.DataSource
            .Where(a => a.IsDirectShip && purchaseOrderItemIds.Contains(a.PurchaseOrderItemId))
            .ToList()
            .Select(a =>
            {
                deliveryNotesByOrderItem.TryGetValue(a.OrderItemId, out var deliveryNote);
                return new DirectShipAllocationForPoItemDto
                {
                    AllocationId = a.Id,
                    PurchaseOrderItemId = a.PurchaseOrderItemId,
                    DirectShipAddress = a.DirectShipAddress ?? string.Empty,
                    DirectShipContactName = a.DirectShipContactName,
                    DirectShipContactPhone = a.DirectShipContactPhone,
                    AllocatedQuantity = a.AllocatedQuantity,
                    ReceivedQuantity = a.ReceivedQuantity,
                    Status = (int)a.Status,
                    DeliveryStatus = deliveryNote is null ? null : (int)deliveryNote.Status,
                    DeliveryNoteId = deliveryNote?.Id,
                    DeliveryNoteCode = deliveryNote?.Code
                };
            })
            .ToList();
        return Task.FromResult(results);
    }

    private PurchaseOrderItem ResolvePurchaseOrderItem(Guid purchaseOrderItemId)
    {
        var purchaseOrderItem = purchaseOrderReader.DataSource
            .SelectMany(purchaseOrder => purchaseOrder.Items)
            .FirstOrDefault(item => item.Id == purchaseOrderItemId);

        if (purchaseOrderItem is null)
            throw new PurchaseOrderItemIsNotFoundException();

        return purchaseOrderItem;
    }

    private Warehouse GetTransitWarehouse()
    {
        var transitWarehouse = warehouseReader.DataSource
            .FirstOrDefault(w => w.WarehouseType == WarehouseType.DirectTransit);

        if (transitWarehouse is null)
            throw new DirectShipTransitWarehouseNotConfiguredException();

        return transitWarehouse;
    }

    public Task<Guid> GetTransitWarehouseIdAsync() => Task.FromResult(GetTransitWarehouse().Id);

    private static void EnsureCanRejectDirectShipDelivery(DeliveryNote deliveryNote)
    {
        if (!deliveryNote.IsDirectShip || deliveryNote.SourceType != DeliveryNoteSourceType.DirectShipToCustomer)
            throw new DeliveryNoteCannotChangeStatusException(deliveryNote.Status, deliveryNote.Status);
        if (deliveryNote.Status != DeliveryNoteStatus.Confirmed)
            throw new DeliveryNoteCannotChangeStatusException(deliveryNote.Status, DeliveryNoteStatus.Cancelled);
    }

    private async Task ReturnDirectShipStockAsync(
        DeliveryNote deliveryNote,
        Guid returnWarehouseId,
        string reason,
        Guid userId = default)
    {
        var returnWarehouse = await ResolveReturnWarehouseAsync(returnWarehouseId).ConfigureAwait(false);

        foreach (var item in deliveryNote.Items)
        {
            await TransferStockWithCostAsync(
                item.ProductId,
                deliveryNote.WarehouseId,
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

    private async Task TransferStockWithCostAsync(
        Guid productId,
        Guid fromWarehouseId,
        Guid toWarehouseId,
        decimal quantity,
        Guid referenceId,
        Guid referenceItemId,
        Guid userId,
        string reason)
    {
        if (fromWarehouseId == toWarehouseId)
            return;

        var costSummary = await inventoryCostingManager.GetCurrentCostSummaryAsync(productId).ConfigureAwait(false);

        await stockManager.TransferStockAsync(
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

    private Dictionary<Guid, DeliveryNote> GetDirectShipDeliveryNotesByOrderItem(IReadOnlyCollection<Guid> orderItemIds)
        => deliveryNoteReader.DataSource
            .Where(d => d.SourceType == DeliveryNoteSourceType.DirectShipToCustomer
                && d.Items.Any(i => orderItemIds.Contains(i.OrderItemId)))
            .SelectMany(d => d.Items.Select(i => new { i.OrderItemId, DeliveryNote = d }))
            .Where(x => orderItemIds.Contains(x.OrderItemId))
            .GroupBy(x => x.OrderItemId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.DeliveryNote.CreatedOnUtc).First().DeliveryNote);

    private static DeliveryNoteDto MapDeliveryNote(DeliveryNote d)
        => new()
        {
            Id = d.Id,
            Code = d.Code,
            OrderId = d.OrderId,
            OrderCode = d.OrderCode,
            WarehouseId = d.WarehouseId,
            CustomerId = d.CustomerId,
            CustomerName = d.CustomerInfo.FullName,
            CustomerPhone = d.CustomerInfo.PhoneNumber,
            CustomerAddress = d.CustomerInfo.Address,
            ShippingAddress = d.ShippingAddress,
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
                ProductName = i.ProductName ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal,
                CostAtDispatch = i.CostAtDispatch
            }).ToList()
        };
}

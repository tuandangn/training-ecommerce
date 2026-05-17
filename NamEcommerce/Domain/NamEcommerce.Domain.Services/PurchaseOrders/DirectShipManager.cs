using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Domain.Services.PurchaseOrders;

public sealed class DirectShipManager(
    IRepository<PurchaseOrderItemAllocation> allocationRepository,
    IEntityDataReader<PurchaseOrderItemAllocation> allocationReader,
    IRepository<DirectShipAddressChangeLog> changeLogRepository,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IRepository<DeliveryNote> deliveryNoteRepository) : IDirectShipManager
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

    public async Task<DistributeReceivedQuantityResultDto> DistributeReceivedQuantityAsync(
        Guid purchaseOrderItemId, decimal receivedQty, CancellationToken ct = default)
    {
        var allocations = allocationReader.DataSource
            .Where(a => a.PurchaseOrderItemId == purchaseOrderItemId
                     && a.Status != AllocationStatus.Cancelled)
            .OrderByDescending(a => a.IsDirectShip)
            .ThenByDescending(a => a.DirectShipPriority)
            .ThenBy(a => a.CreatedOnUtc)
            .ToList();

        var directShipReceipts = new List<AllocationReceiptDto>();
        var warehouseReceipts = new List<AllocationReceiptDto>();
        var remaining = receivedQty;

        foreach (var allocation in allocations)
        {
            if (remaining <= 0) break;

            var canReceive = allocation.AllocatedQuantity - allocation.ReceivedQuantity;
            if (canReceive <= 0) continue;

            var toReceive = Math.Min(remaining, canReceive);
            allocation.IncreaseReceived(toReceive);
            await allocationRepository.UpdateAsync(allocation, ct).ConfigureAwait(false);

            var receipt = new AllocationReceiptDto
            {
                AllocationId = allocation.Id,
                OrderItemId = allocation.OrderItemId,
                Quantity = toReceive,
                IsDirectShip = allocation.IsDirectShip,
                DirectShipAddress = allocation.DirectShipAddress
            };

            if (allocation.IsDirectShip)
                directShipReceipts.Add(receipt);
            else
                warehouseReceipts.Add(receipt);

            remaining -= toReceive;
        }

        return new DistributeReceivedQuantityResultDto
        {
            DirectShipReceipts = directShipReceipts,
            WarehouseReceipts = warehouseReceipts
        };
    }

    public async Task ConfirmDeliveryAsync(
        Guid deliveryNoteId, DateTime confirmedAtUtc, string? note, CancellationToken ct = default)
    {
        var deliveryNote = await deliveryNoteReader.GetByIdAsync(deliveryNoteId)
            ?? throw new DeliveryNoteNotFoundException(deliveryNoteId);

        deliveryNote.ConfirmDirectShipDelivery(confirmedAtUtc, note);
        await deliveryNoteRepository.UpdateAsync(deliveryNote, ct).ConfigureAwait(false);
    }

    public async Task RejectDeliveryAsync(
        Guid deliveryNoteId, string reason, CancellationToken ct = default)
    {
        var deliveryNote = await deliveryNoteReader.GetByIdAsync(deliveryNoteId)
            ?? throw new DeliveryNoteNotFoundException(deliveryNoteId);

        deliveryNote.RejectDirectShipDelivery(reason);
        await deliveryNoteRepository.UpdateAsync(deliveryNote, ct).ConfigureAwait(false);
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
            .Where(d => d.IsDirectShip && d.DeliveryConfirmationStatus == DeliveryConfirmationStatus.PendingConfirmation);

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            var kw = keywords.Trim().ToLower();
            query = query.Where(d =>
                d.Code.ToLower().Contains(kw) ||
                d.CustomerName.ToLower().Contains(kw) ||
                d.ShippingAddress.ToLower().Contains(kw));
        }

        if (fromDateUtc.HasValue)
            query = query.Where(d => d.CreatedOnUtc >= fromDateUtc.Value);

        if (toDateUtc.HasValue)
            query = query.Where(d => d.CreatedOnUtc <= toDateUtc.Value);

        var results = query
            .OrderByDescending(d => d.CreatedOnUtc)
            .ToList();

        IList<DeliveryNoteDto> dtos = results.Select(d => new DeliveryNoteDto
        {
            Id = d.Id,
            Code = d.Code,
            OrderId = d.OrderId,
            OrderCode = d.OrderCode,
            WarehouseId = d.WarehouseId,
            CustomerId = d.CustomerId,
            CustomerName = d.CustomerName,
            CustomerPhone = d.CustomerPhone,
            CustomerAddress = d.CustomerAddress,
            ShippingAddress = d.ShippingAddress,
            ShowPrice = d.ShowPrice,
            Note = d.Note,
            Status = d.Status,
            SourceType = d.SourceType,
            IsDirectShip = d.IsDirectShip,
            DeliveryConfirmationStatus = d.DeliveryConfirmationStatus,
            CreatedOnUtc = d.CreatedOnUtc,
            TotalAmount = d.TotalAmount,
            Items = d.Items.Select(i => new DeliveryNoteItemDto
            {
                Id = i.Id,
                DeliveryNoteId = i.DeliveryNoteId,
                OrderItemId = i.OrderItemId,
                ProductId = i.ProductId,
                ProductName = i.ProductName ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
            }).ToList()
        }).ToList();

        return Task.FromResult(dtos);
    }

    public Task<IList<DirectShipAllocationStatusDto>> GetDirectShipAllocationsForOrderItemsAsync(
        IReadOnlyList<Guid> orderItemIds, CancellationToken ct = default)
    {
        IList<DirectShipAllocationStatusDto> results = allocationReader.DataSource
            .Where(a => a.IsDirectShip && orderItemIds.Contains(a.OrderItemId))
            .Select(a => new DirectShipAllocationStatusDto
            {
                AllocationId = a.Id,
                OrderItemId = a.OrderItemId,
                Status = (int)a.Status,
                AllocatedQuantity = a.AllocatedQuantity
            })
            .ToList();
        return Task.FromResult(results);
    }

    public Task<IList<DirectShipAllocationForPoItemDto>> GetDirectShipAllocationsForPoItemsAsync(
        IReadOnlyList<Guid> purchaseOrderItemIds, CancellationToken ct = default)
    {
        IList<DirectShipAllocationForPoItemDto> results = allocationReader.DataSource
            .Where(a => a.IsDirectShip && purchaseOrderItemIds.Contains(a.PurchaseOrderItemId))
            .Select(a => new DirectShipAllocationForPoItemDto
            {
                AllocationId = a.Id,
                PurchaseOrderItemId = a.PurchaseOrderItemId,
                DirectShipAddress = a.DirectShipAddress ?? string.Empty,
                DirectShipContactName = a.DirectShipContactName,
                DirectShipContactPhone = a.DirectShipContactPhone,
                AllocatedQuantity = a.AllocatedQuantity,
                Status = (int)a.Status
            })
            .ToList();
        return Task.FromResult(results);
    }
}

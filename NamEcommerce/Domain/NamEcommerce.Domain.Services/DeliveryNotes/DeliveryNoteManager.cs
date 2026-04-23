using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;

namespace NamEcommerce.Domain.Services.DeliveryNotes;

public sealed class DeliveryNoteManager(
    IRepository<DeliveryNote> deliveryNoteRepository,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IEntityDataReader<Order> orderReader,
    IRepository<Order> orderRepository,
    IInventoryStockManager stockManager,
    IEventPublisher eventPublisher) : IDeliveryNoteManager
{
    private Task<string> GenerateCodeAsync()
    {
        var datePrefix = $"PXK-{DateTime.UtcNow:yyyyMMdd}";
        var count = deliveryNoteReader.DataSource.Count(d => d.Code.StartsWith(datePrefix));
        return Task.FromResult($"{datePrefix}-{(count + 1):D3}");
    }

    public async Task<DeliveryNoteDto> CreateFromOrderAsync(CreateDeliveryNoteDto dto)
    {
        var order = await orderReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        var code = await GenerateCodeAsync().ConfigureAwait(false);

        var deliveryNote = new DeliveryNote(
            code: code,
            orderId: order.Id,
            customerId: order.CustomerId,
            customerName: order.CustomerName ?? string.Empty,
            customerPhone: order.CustomerPhone,
            customerAddress: order.CustomerAddress,
            shippingAddress: dto.ShippingAddress,
            warehouseId: dto.WarehouseId,
            showPrice: dto.ShowPrice,
            note: dto.Note,
            surcharge: dto.Surcharge,
            amountToCollect: dto.AmountToCollect,
            surchargeReason: dto.SurchargeReason,
            createdByUserId: dto.CreatedByUserId
        )
        {
            OrderCode = order.Code,
            WarehouseName = dto.WarehouseName
        };

        foreach (var itemDto in dto.Items)
        {
            var orderItem = order.OrderItems.FirstOrDefault(i => i.Id == itemDto.OrderItemId);
            if (orderItem is null)
                throw new OrderItemIsNotFoundException(itemDto.OrderItemId);

            deliveryNote.AddItem(
                orderItemId: orderItem.Id,
                productId: orderItem.ProductId,
                productName: orderItem.ProductName ?? string.Empty,
                quantity: itemDto.Quantity,
                unitPrice: orderItem.UnitPrice // Taking unit price from order item so total amount is correct
            );
        }

        var inserted = await deliveryNoteRepository.InsertAsync(deliveryNote).ConfigureAwait(false);

        await eventPublisher.EntityCreated(inserted).ConfigureAwait(false);
        
        return MapToDto(inserted);
    }

    public async Task ConfirmAsync(Guid id)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        // Reserve stock for all items
        foreach (var item in deliveryNote.Items)
        {
            await stockManager.ReserveStockAsync(
                item.ProductId, 
                deliveryNote.WarehouseId, 
                item.Quantity, 
                deliveryNote.Id, 
                Guid.Empty, // Default user for now
                $"Giữ hàng cho phiếu xuất {deliveryNote.Code}").ConfigureAwait(false);
        }

        deliveryNote.Confirm();
        var updated = await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);

        await eventPublisher.PublishEvent(new DeliveryNoteConfirmedEvent(deliveryNote.Id)).ConfigureAwait(false);
    }

    public async Task MarkDeliveringAsync(Guid id)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        deliveryNote.MarkDelivering();
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
    }

    public async Task MarkDeliveredAsync(MarkDeliveryNoteDeliveredDto dto)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(dto.DeliveryNoteId);

        // 1. Mark DeliveryNote Delivered
        deliveryNote.MarkDelivered(dto.PictureId, dto.ReceiverName);
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);

        // 2. Dispatch stock (Deduct from QuantityOnHand and clear reservation)
        foreach (var item in deliveryNote.Items)
        {
            await stockManager.DispatchStockAsync(
                item.ProductId,
                deliveryNote.WarehouseId,
                item.Quantity,
                deliveryNote.Id,
                Guid.Empty, // Default user
                $"Xuất hàng cho phiếu xuất {deliveryNote.Code}").ConfigureAwait(false);
        }

        // 2. Mark related OrderItems as Delivered
        var order = await orderRepository.GetByIdAsync(deliveryNote.OrderId).ConfigureAwait(false);
        if (order is not null)
        {
            foreach (var noteItem in deliveryNote.Items)
            {
                // Only mark as delivered if the entire requested quantity was delivered
                // For simplicity, any delivery note item delivered marks the whole order item delivered currently
                var orderItem = order.OrderItems.FirstOrDefault(i => i.Id == noteItem.OrderItemId);
                if (orderItem != null && !orderItem.IsDelivered)
                {
                    order.MarkOrderItemDelivered(orderItem.Id, dto.PictureId);
                }
            }

            // 3. Try to Auto Lock
            order.TryAutoLock();
            await orderRepository.UpdateAsync(order).ConfigureAwait(false);
        }

        await eventPublisher.PublishEvent(new DeliveryNoteDeliveredEvent(deliveryNote.Id)).ConfigureAwait(false);
    }

    public async Task CancelAsync(Guid id)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        bool wasConfirmed = deliveryNote.Status == DeliveryNoteStatus.Confirmed || deliveryNote.Status == DeliveryNoteStatus.Delivering;

        deliveryNote.Cancel();
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);

        // If it was confirmed/delivering, release the reserved stock
        if (wasConfirmed)
        {
            foreach (var item in deliveryNote.Items)
            {
                await stockManager.ReleaseReservedStockAsync(
                    item.ProductId,
                    deliveryNote.WarehouseId,
                    item.Quantity,
                    deliveryNote.Id,
                    Guid.Empty,
                    $"Giải phóng hàng phiếu xuất {deliveryNote.Code} bị hủy").ConfigureAwait(false);
            }
        }
    }

    public async Task<DeliveryNoteDto?> GetByIdAsync(Guid id)
    {
        var baseQuery = deliveryNoteReader.DataSource; // Eager loading should happen here if possible, but IEntityDataReader usually handles it
        // Or we map manually. Assuming GetByIdAsync works normally but doesn't eager load Items if not configured.
        // Actually, we should just query and map.
        var deliveryNote = await deliveryNoteReader.GetByIdAsync(id).ConfigureAwait(false);
        return deliveryNote is null ? null : MapToDto(deliveryNote);
    }

    public async Task<IPagedDataDto<DeliveryNoteDto>> GetDeliveryNotesAsync(int pageIndex, int pageSize, string? keywords, Guid? orderId, IEnumerable<DeliveryNoteStatus>? status)
    {
        var query = deliveryNoteReader.DataSource;
        
        if (!string.IsNullOrWhiteSpace(keywords))
        {
            var uppercaseKeywords = keywords.Trim().ToUpper();
            query = query.Where(deliveryNote => 
                deliveryNote.Code.Contains(keywords) || 
                (deliveryNote.OrderCode != null && deliveryNote.OrderCode.Contains(keywords)) ||
                deliveryNote.CustomerName.ToUpper().Contains(uppercaseKeywords) ||
                (deliveryNote.CustomerPhone != null && deliveryNote.CustomerPhone.Contains(keywords))
            );
        }
        if (orderId.HasValue)
            query = query.Where(deliverNote => deliverNote.OrderId == orderId);
        if(status != null && status.Any())
            query = query.Where(deliverNote => status.Contains(deliverNote.Status));

        query = query.OrderByDescending(x => x.CreatedOnUtc);

        var total = query.Count();
        if (total == 0)
        {
            return PagedDataDto.Create(new List<DeliveryNoteDto>(), pageIndex, pageSize, 0);
        }

        var deliveryNotes = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        
        return PagedDataDto.Create(deliveryNotes.Select(MapToDto).ToList(), pageIndex, pageSize, total);
    }

    public Task<IDictionary<Guid, decimal>> GetDeliveredQuantitiesAsync(IEnumerable<Guid> orderItemIds)
    {
        var ids = orderItemIds.ToList();
        if (ids.Count == 0)
        {
            return Task.FromResult<IDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());
        }

        var deliveredQuantities = deliveryNoteReader.DataSource
            .Where(x => x.Status != DeliveryNoteStatus.Cancelled)
            .SelectMany(x => x.Items)
            .Where(x => ids.Contains(x.OrderItemId))
            .GroupBy(x => x.OrderItemId)
            .Select(g => new { OrderItemId = g.Key, DeliveredQuantity = g.Sum(x => x.Quantity) })
            .ToList();

        IDictionary<Guid, decimal> result = deliveredQuantities.ToDictionary(x => x.OrderItemId, x => x.DeliveredQuantity);

        foreach (var id in ids)
        {
            if (!result.ContainsKey(id))
            {
                result[id] = 0;
            }
        }

        return Task.FromResult(result);
    }

    public Task<IDictionary<Guid, List<DeliveryNoteLinkDto>>> GetDeliveryNoteLinksAsync(IEnumerable<Guid> orderItemIds)
    {
        var ids = orderItemIds.ToList();
        if (ids.Count == 0)
        {
            return Task.FromResult<IDictionary<Guid, List<DeliveryNoteLinkDto>>>(new Dictionary<Guid, List<DeliveryNoteLinkDto>>());
        }

        var links = deliveryNoteReader.DataSource
            .Where(x => x.Status != DeliveryNoteStatus.Cancelled)
            .SelectMany(x => x.Items.Select(i => new { i.OrderItemId, x.Id, x.Code, x.Status, x.CreatedOnUtc }))
            .Where(x => ids.Contains(x.OrderItemId))
            .ToList()
            .GroupBy(x => x.OrderItemId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new DeliveryNoteLinkDto(x.Id, x.Code, x.Status, x.CreatedOnUtc)).Distinct().ToList()
            );

        foreach (var id in ids)
        {
            if (!links.ContainsKey(id))
            {
                links[id] = [];
            }
        }

        return Task.FromResult<IDictionary<Guid, List<DeliveryNoteLinkDto>>>(links);
    }

    private static DeliveryNoteDto MapToDto(DeliveryNote deliveryNote)
    {
        return new DeliveryNoteDto
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            OrderId = deliveryNote.OrderId,
            WarehouseId = deliveryNote.WarehouseId,
            OrderCode = deliveryNote.OrderCode,
            CustomerId = deliveryNote.CustomerId,
            CustomerName = deliveryNote.CustomerName ?? string.Empty,
            CustomerPhone = deliveryNote.CustomerPhone,
            CustomerAddress = deliveryNote.CustomerAddress,
            ShippingAddress = deliveryNote.ShippingAddress,
            ShowPrice = deliveryNote.ShowPrice,
            Note = deliveryNote.Note,
            Status = deliveryNote.Status,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc,
            DeliveryProofPictureId = deliveryNote.DeliveryProofPictureId,
            DeliveryReceiverName = deliveryNote.DeliveryReceiverName,
            CreatedByUserId = deliveryNote.CreatedByUserId,
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            UpdatedOnUtc = deliveryNote.UpdatedOnUtc,
            TotalAmount = deliveryNote.TotalAmount,
            Surcharge = deliveryNote.Surcharge,
            SurchargeReason = deliveryNote.SurchargeReason,
            AmountToCollect = deliveryNote.AmountToCollect,
            Items = deliveryNote.Items.Select(i => new DeliveryNoteItemDto
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
        };
    }
}

using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;

namespace NamEcommerce.Domain.Services.DeliveryNotes;

public sealed class DeliveryNoteManager : IDeliveryNoteManager
{
    private readonly IRepository<DeliveryNote> _deliveryNoteRepository;
    private readonly IEntityDataReader<DeliveryNote> _deliveryNoteReader;
    private readonly IEntityDataReader<Order> _orderReader;
    private readonly IRepository<Order> _orderRepository;

    public DeliveryNoteManager(
        IRepository<DeliveryNote> deliveryNoteRepository,
        IEntityDataReader<DeliveryNote> deliveryNoteReader,
        IEntityDataReader<Order> orderReader,
        IRepository<Order> orderRepository)
    {
        _deliveryNoteRepository = deliveryNoteRepository;
        _deliveryNoteReader = deliveryNoteReader;
        _orderReader = orderReader;
        _orderRepository = orderRepository;
    }

    private Task<string> GenerateCodeAsync()
    {
        var datePrefix = $"PXK-{DateTime.UtcNow:yyyyMMdd}";
        var count = _deliveryNoteReader.DataSource.Count(d => d.Code.StartsWith(datePrefix));
        return Task.FromResult($"{datePrefix}-{(count + 1):D3}");
    }

    public async Task<DeliveryNoteDto> CreateFromOrderAsync(CreateDeliveryNoteDto dto)
    {
        var order = await _orderReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        var code = await GenerateCodeAsync().ConfigureAwait(false);

        var deliveryNote = new DeliveryNote(
            code: code,
            orderId: order.Id,
            customerId: order.CustomerId,
            customerName: order.CustomerName,
            customerPhone: order.CustomerPhone,
            customerAddress: order.CustomerAddress,
            shippingAddress: dto.ShippingAddress,
            showPrice: dto.ShowPrice,
            note: dto.Note,
            createdByUserId: dto.CreatedByUserId
        );

        foreach (var itemDto in dto.Items)
        {
            var orderItem = order.OrderItems.FirstOrDefault(i => i.Id == itemDto.OrderItemId);
            if (orderItem is null)
                throw new OrderItemIsNotFoundException(itemDto.OrderItemId);

            deliveryNote.AddItem(
                orderItemId: orderItem.Id,
                productId: orderItem.ProductId,
                productName: orderItem.ProductName,
                quantity: itemDto.Quantity,
                unitPrice: orderItem.UnitPrice // Taking unit price from order item so total amount is correct
            );
        }

        var inserted = await _deliveryNoteRepository.InsertAsync(deliveryNote).ConfigureAwait(false);
        
        return MapToDto(inserted);
    }

    public async Task ConfirmAsync(Guid id)
    {
        var note = await _deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (note is null)
            throw new DeliveryNoteNotFoundException(id);

        note.Confirm();
        await _deliveryNoteRepository.UpdateAsync(note).ConfigureAwait(false);
    }

    public async Task MarkDeliveringAsync(Guid id)
    {
        var note = await _deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (note is null)
            throw new DeliveryNoteNotFoundException(id);

        note.MarkDelivering();
        await _deliveryNoteRepository.UpdateAsync(note).ConfigureAwait(false);
    }

    public async Task MarkDeliveredAsync(MarkDeliveryNoteDeliveredDto dto)
    {
        var note = await _deliveryNoteRepository.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false);
        if (note is null)
            throw new DeliveryNoteNotFoundException(dto.DeliveryNoteId);

        // 1. Mark DeliveryNote Delivered
        note.MarkDelivered(dto.PictureId, dto.ReceiverName);
        await _deliveryNoteRepository.UpdateAsync(note).ConfigureAwait(false);

        // 2. Mark related OrderItems as Delivered
        var order = await _orderRepository.GetByIdAsync(note.OrderId).ConfigureAwait(false);
        if (order is not null)
        {
            foreach (var noteItem in note.Items)
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
            await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
        }
    }

    public async Task CancelAsync(Guid id)
    {
        var note = await _deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (note is null)
            throw new DeliveryNoteNotFoundException(id);

        note.Cancel();
        await _deliveryNoteRepository.UpdateAsync(note).ConfigureAwait(false);
    }

    public async Task<DeliveryNoteDto?> GetByIdAsync(Guid id)
    {
        var baseQuery = _deliveryNoteReader.DataSource; // Eager loading should happen here if possible, but IEntityDataReader usually handles it
        // Or we map manually. Assuming GetByIdAsync works normally but doesn't eager load Items if not configured.
        // Actually, we should just query and map.
        var note = await _deliveryNoteReader.GetByIdAsync(id).ConfigureAwait(false);
        return note is null ? null : MapToDto(note);
    }

    public async Task<IPagedDataDto<DeliveryNoteDto>> GetListAsync(string? keywords = null, int pageIndex = 0, int pageSize = 15)
    {
        var query = _deliveryNoteReader.DataSource;
        
        if (!string.IsNullOrWhiteSpace(keywords))
        {
            var lower = keywords.ToLower();
            query = query.Where(x => 
                x.Code.ToLower().Contains(lower) || 
                x.CustomerName.ToLower().Contains(lower) ||
                (x.CustomerPhone != null && x.CustomerPhone.Contains(lower))
            );
        }

        query = query.OrderByDescending(x => x.CreatedOnUtc);

        var total = query.Count();
        if (total == 0)
        {
            return PagedDataDto.Create<DeliveryNoteDto>(new List<DeliveryNoteDto>(), pageIndex, pageSize, 0);
        }

        var items = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        
        return PagedDataDto.Create<DeliveryNoteDto>(items.Select(MapToDto).ToList(), pageIndex, pageSize, total);
    }

    private static DeliveryNoteDto MapToDto(DeliveryNote source)
    {
        return new DeliveryNoteDto
        {
            Id = source.Id,
            Code = source.Code,
            OrderId = source.OrderId,
            CustomerId = source.CustomerId,
            CustomerName = source.CustomerName ?? string.Empty,
            CustomerPhone = source.CustomerPhone,
            CustomerAddress = source.CustomerAddress,
            ShippingAddress = source.ShippingAddress,
            ShowPrice = source.ShowPrice,
            Note = source.Note,
            Status = source.Status,
            DeliveredOnUtc = source.DeliveredOnUtc,
            DeliveryProofPictureId = source.DeliveryProofPictureId,
            DeliveryReceiverName = source.DeliveryReceiverName,
            CreatedByUserId = source.CreatedByUserId,
            CreatedOnUtc = source.CreatedOnUtc,
            UpdatedOnUtc = source.UpdatedOnUtc,
            TotalAmount = source.TotalAmount,
            Items = source.Items.Select(i => new DeliveryNoteItemDto
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

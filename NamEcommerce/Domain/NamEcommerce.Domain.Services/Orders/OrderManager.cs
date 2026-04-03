using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Services.Extensions;

namespace NamEcommerce.Domain.Services.Orders;

public sealed class OrderManager : IOrderManager
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IEntityDataReader<Order> _orderDataReader;
    private readonly IInventoryStockManager _stockManager;

    public OrderManager(IRepository<Order> orderRepository, IEntityDataReader<Order> orderDataReader, IInventoryStockManager stockManager)
    {
        _orderRepository = orderRepository;
        _orderDataReader = orderDataReader;
        _stockManager = stockManager;
    }

    public async Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, err) = dto.Items.Select(i => i.Validate()).FirstOrDefault(r => !r.valid) is var invalid && invalid.valid == false
            ? (false, invalid.message)
            : (true, (string?)null);

        if (!valid)
            throw new ArgumentException(err);

        var items = dto.Items.Select(i => new OrderItem(Guid.NewGuid(), Guid.Empty, i.ProductId, i.UnitPrice, i.Quantity)).ToList();
        var total = items.Sum(i => i.Price);
        var order = new Order(Guid.NewGuid(), dto.CustomerId ?? Guid.Empty, dto.WarehouseId, total, 0, 0, items)
        {
            OrderDiscount = dto.OrderDiscount ?? 0,
            Note = dto.Note
        };

        var inserted = await _orderRepository.InsertAsync(order).ConfigureAwait(false);

        // Reserve Stock if warehouse is specified
        if (order.WarehouseId.HasValue)
        {
            foreach (var item in order.OrderItems)
            {
                 // ReferenceId is order.Id, userId is Guid.Empty (system) for now
                 await _stockManager.ReserveStockAsync(item.ProductId, order.WarehouseId.Value, item.Quantity, order.Id, Guid.Empty, "Sales Order Reservation").ConfigureAwait(false);
            }
        }

        return new CreateOrderResultDto { CreatedId = inserted.Id };
    }

    public async Task<UpdateOrderResultDto> UpdateOrderAsync(UpdateOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await _orderDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (order is null)
            throw new ArgumentException("Order is not found", nameof(dto));

        if (dto.OrderDiscount.HasValue)
            order.OrderDiscount = dto.OrderDiscount.Value;
            
        order.Note = dto.Note;
        order.UpdatedOnUtc = DateTime.UtcNow;

        var updated = await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
        return new UpdateOrderResultDto { UpdatedId = updated.Id };
    }

    public async Task AddOrderItemAsync(AddOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(dto));

        var order = await _orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new ArgumentException("Order is not found", nameof(dto.OrderId));

        var item = new OrderItem(Guid.NewGuid(), dto.OrderId, dto.ProductId, dto.UnitPrice, dto.Quantity);
        order.AddOrderItem(item);
        order.UpdatedOnUtc = DateTime.UtcNow;

        await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task UpdateOrderItemAsync(UpdateOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await _orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null) throw new ArgumentException("Order not found");

        order.UpdateOrderItem(dto.ItemId, dto.Quantity, dto.UnitPrice);
        order.UpdatedOnUtc = DateTime.UtcNow;

        await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task DeleteOrderItemAsync(DeleteOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await _orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null) throw new ArgumentException("Order not found");

        order.RemoveOrderItem(dto.ItemId);
        order.UpdatedOnUtc = DateTime.UtcNow;

        await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task ChangeOrderStatusAsync(Guid orderId, int status)
    {
        var order = await _orderDataReader.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new ArgumentException("Order is not found", nameof(orderId));

        if (!Enum.IsDefined(typeof(OrderStatus), status))
            throw new ArgumentException("Invalid status", nameof(status));

        var fromStatus = order.OrderStatus;
        var toStatus = (OrderStatus)status;
        
        if (fromStatus == toStatus) return;

        order.ChangeStatus(toStatus);
        order.UpdatedOnUtc = DateTime.UtcNow;

        // Inventory Integration
        if (order.WarehouseId.HasValue)
        {
            if (toStatus == OrderStatus.Completed)
            {
                // Dispatch stock (Deduct)
                foreach (var item in order.OrderItems)
                {
                    await _stockManager.DispatchStockAsync(item.ProductId, order.WarehouseId.Value, item.Quantity, order.Id, Guid.Empty, $"Sales Order {order.Id} Completed").ConfigureAwait(false);
                }
            }
            else if (toStatus == OrderStatus.Cancelled && fromStatus != OrderStatus.Completed)
            {
                // Release reservation
                foreach (var item in order.OrderItems)
                {
                    await _stockManager.ReleaseReservedStockAsync(item.ProductId, order.WarehouseId.Value, item.Quantity, order.Id, Guid.Empty, "Sales Order Cancelled").ConfigureAwait(false);
                }
            }
        }

        await _orderRepository.UpdateAsync(order).ConfigureAwait(false);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid id)
    {
        var order = await _orderDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (order is null) return null;
        return order.ToDto();
    }

    public async Task<IPagedDataDto<OrderDto>> GetOrdersAsync(string? keywords, int pageIndex, int pageSize)
    {
        var query = _orderDataReader.DataSource;
        var items = query.Select(o => o.ToDto());
        var total = items.Count();
        var paged = items.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        var result = NamEcommerce.Domain.Shared.Dtos.Common.PagedDataDto.Create(paged, pageIndex, pageSize, total);
        return result;
    }
}

using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Domain.Shared.Services.Catalog;

namespace NamEcommerce.Application.Services.Orders;

public sealed class OrderAppService : IOrderAppService
{
    private readonly IOrderManager _orderManager;
    private readonly ICustomerManager _customerManager;
    private readonly IWarehouseManager _warehouseManager;

    public OrderAppService(IOrderManager orderManager, ICustomerManager customerManager, IWarehouseManager warehouseManager)
    {
        _orderManager = orderManager;
        _customerManager = customerManager;
        _warehouseManager = warehouseManager;
    }

    public async Task<UpdateOrderResultAppDto> UpdateOrderAsync(UpdateOrderAppDto dto)
    {
        var domainDto = new NamEcommerce.Domain.Shared.Dtos.Orders.UpdateOrderDto(dto.Id)
        {
            Note = dto.Note,
            OrderDiscount = dto.OrderDiscount
        };
        var result = await _orderManager.UpdateOrderAsync(domainDto).ConfigureAwait(false);
        return new UpdateOrderResultAppDto { Success = true, UpdatedId = result.UpdatedId };
    }

    public async Task<AddOrderItemResultAppDto> AddOrderItemAsync(AddOrderItemAppDto dto)
    {
        var domainDto = new NamEcommerce.Domain.Shared.Dtos.Orders.AddOrderItemDto(dto.OrderId, dto.ProductId, dto.Quantity, dto.UnitPrice);
        await _orderManager.AddOrderItemAsync(domainDto).ConfigureAwait(false);
        return new AddOrderItemResultAppDto { Success = true };
    }

    public async Task<UpdateOrderItemResultAppDto> UpdateOrderItemAsync(UpdateOrderItemAppDto dto)
    {
        var domainDto = new NamEcommerce.Domain.Shared.Dtos.Orders.UpdateOrderItemDto(dto.OrderId, dto.ItemId, dto.Quantity, dto.UnitPrice);
        await _orderManager.UpdateOrderItemAsync(domainDto).ConfigureAwait(false);
        return new UpdateOrderItemResultAppDto { Success = true };
    }

    public async Task<DeleteOrderItemResultAppDto> DeleteOrderItemAsync(DeleteOrderItemAppDto dto)
    {
        var domainDto = new NamEcommerce.Domain.Shared.Dtos.Orders.DeleteOrderItemDto(dto.OrderId, dto.ItemId);
        await _orderManager.DeleteOrderItemAsync(domainDto).ConfigureAwait(false);
        return new DeleteOrderItemResultAppDto { Success = true };
    }

    public Task ChangeOrderStatusAsync(Guid orderId, int status)
    {
        return _orderManager.ChangeOrderStatusAsync(orderId, status);
    }

    public async Task<OrderAppDto?> GetOrderByIdAsync(Guid id)
    {
        var dto = await _orderManager.GetOrderByIdAsync(id).ConfigureAwait(false);
        if (dto is null) return null;
        
        var customer = dto.CustomerId.HasValue ? await _customerManager.GetCustomerByIdAsync(dto.CustomerId.Value).ConfigureAwait(false) : null;
        var warehouse = dto.WarehouseId.HasValue ? await _warehouseManager.GetWarehouseByIdAsync(dto.WarehouseId.Value).ConfigureAwait(false) : null;

        var appDto = new OrderAppDto(dto.Id)
        {
            CustomerId = dto.CustomerId ?? Guid.Empty,
            CustomerName = customer?.FullName ?? "N/A",
            WarehouseId = dto.WarehouseId,
            WarehouseName = warehouse?.Name ?? "N/A",
            TotalAmount = dto.TotalAmount,
            OrderDiscount = dto.OrderDiscount ?? 0,
            Status = dto.Status,
            Note = dto.Note
        };
        foreach (var it in dto.Items)
            appDto.Items.Add(new OrderItemAppDto(it.ItemId, it.ProductId, it.Quantity, it.UnitPrice));
            
        return appDto;
    }

    public async Task<NamEcommerce.Application.Contracts.Dtos.Common.IPagedDataAppDto<OrderAppDto>> GetOrdersAsync(string? keywords, int pageIndex, int pageSize)
    {
        var paged = await _orderManager.GetOrdersAsync(keywords, pageIndex, pageSize).ConfigureAwait(false);
        
        var items = new List<OrderAppDto>();
        foreach (var o in paged.Items)
        {
            var customer = o.CustomerId.HasValue ? await _customerManager.GetCustomerByIdAsync(o.CustomerId.Value).ConfigureAwait(false) : null;
            var warehouse = o.WarehouseId.HasValue ? await _warehouseManager.GetWarehouseByIdAsync(o.WarehouseId.Value).ConfigureAwait(false) : null;
            
            items.Add(new OrderAppDto(o.Id)
            {
                CustomerId = o.CustomerId ?? Guid.Empty,
                CustomerName = customer?.FullName ?? "N/A",
                WarehouseId = o.WarehouseId,
                WarehouseName = warehouse?.Name ?? "N/A",
                TotalAmount = o.TotalAmount,
                OrderDiscount = o.OrderDiscount ?? 0,
                Status = o.Status,
                Note = o.Note
            });
        }
        
        return NamEcommerce.Application.Contracts.Dtos.Common.PagedDataAppDto.Create(items, pageIndex, pageSize, paged.PagerInfo.TotalCount);
    }

    public async Task<CreateOrderResultAppDto> CreateOrderAsync(CreateOrderAppDto dto)
    {
        var createDto = new NamEcommerce.Domain.Shared.Dtos.Orders.CreateOrderDto
        {
            CustomerId = dto.CustomerId,
            WarehouseId = dto.WarehouseId,
            Note = dto.Note,
            OrderDiscount = dto.OrderDiscount
        };
        foreach (var it in dto.Items)
            createDto.Items.Add(new NamEcommerce.Domain.Shared.Dtos.Orders.OrderItemDto(Guid.Empty, it.ProductId, it.Quantity, it.UnitPrice));

        var result = await _orderManager.CreateOrderAsync(createDto).ConfigureAwait(false);

        return new CreateOrderResultAppDto
        {
            Success = true,
            CreatedId = result.CreatedId
        };
    }
}


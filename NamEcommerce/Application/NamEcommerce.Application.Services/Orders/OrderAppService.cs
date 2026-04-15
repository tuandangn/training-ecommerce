using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;

namespace NamEcommerce.Application.Services.Orders;

public sealed class OrderAppService(
    IOrderManager orderManager,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<Customer> customerDataReader,
    IEntityDataReader<User> userDataReader) : IOrderAppService
{

    public async Task<UpdateOrderResultAppDto> UpdateOrderAsync(UpdateOrderAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new UpdateOrderResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var order = await orderManager.GetOrderByIdAsync(dto.Id).ConfigureAwait(false);
        if (order is null)
        {
            return new UpdateOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Order is not found."
            };
        }

        if (!order.CanUpdateInfo)
        {
            return new UpdateOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Order cannot update info."
            };
        }

        if ((dto.OrderDiscount ?? 0) > order.OrderSubTotal)
        {
            return new UpdateOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Order discount cannot exceed order sub total."
            };
        }

        var updateResultDto = await orderManager.UpdateOrderAsync(new UpdateOrderDto(dto.Id)
        {
            Note = dto.Note,
            OrderDiscount = dto.OrderDiscount,
            ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc
        }).ConfigureAwait(false);

        return new UpdateOrderResultAppDto
        {
            Success = true,
            UpdatedId = updateResultDto.UpdatedId
        };
    }

    public async Task<AddOrderItemResultAppDto> AddOrderItemAsync(AddOrderItemAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new AddOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var order = await orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new AddOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Order is not found."
            };
        }

        if (!order.CanUpdateOrderItems)
        {
            return new AddOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Order cannot add items."
            };
        }

        var product = await productDataReader.GetByIdAsync(dto.ProductId).ConfigureAwait(false);
        if (product is null)
        {
            return new AddOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Product is not found."
            };
        }

        await orderManager.AddOrderItemAsync(dto.OrderId, new AddOrderItemDto
        {
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice
        }).ConfigureAwait(false);

        return new AddOrderItemResultAppDto
        {
            Success = true,
            OrderId = order.Id
        };
    }

    public async Task<UpdateOrderItemResultAppDto> UpdateOrderItemAsync(UpdateOrderItemAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new UpdateOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var order = await orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new UpdateOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Order is not found."
            };
        }

        if (!order.CanUpdateOrderItems)
        {
            return new UpdateOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Order cannot update items."
            };
        }

        var orderItem = order.Items.FirstOrDefault(item => item.Id == dto.OrderItemId);
        if (orderItem is null)
        {
            return new UpdateOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Order item is not found."
            };
        }

        var calculatedOrderSubTotal = order.OrderSubTotal - orderItem.SubTotal + dto.Quantity * dto.UnitPrice;
        if ((order.OrderDiscount ?? 0) > calculatedOrderSubTotal)
        {
            return new UpdateOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Order discount cannot exceed order sub total."
            };
        }

        await orderManager.UpdateOrderItemAsync(new UpdateOrderItemDto
        {
            OrderId = dto.OrderId,
            OrderItemId = dto.OrderItemId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice
        }).ConfigureAwait(false);

        return new UpdateOrderItemResultAppDto
        {
            Success = true,
            OrderId = dto.OrderId,
            UpdatedItemId = dto.OrderItemId
        };
    }

    public async Task<DeleteOrderItemResultAppDto> DeleteOrderItemAsync(DeleteOrderItemAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new DeleteOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Order is not found."
            };
        }

        if (!order.CanUpdateOrderItems)
        {
            return new DeleteOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Order cannot delete items."
            };
        }

        var orderItem = order.Items.FirstOrDefault(item => item.Id == dto.OrderItemId);
        if (orderItem is null)
        {
            return new DeleteOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Order item is not found."
            };
        }

        var calculatedOrderSubTotal = order.OrderSubTotal - orderItem.SubTotal;
        if ((order.OrderDiscount ?? 0) > calculatedOrderSubTotal)
        {
            return new DeleteOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Order discount cannot exceed order sub total."
            };
        }

        await orderManager.DeleteOrderItemAsync(new DeleteOrderItemDto(dto.OrderId, dto.OrderItemId)).ConfigureAwait(false);

        return new DeleteOrderItemResultAppDto
        {
            Success = true
        };
    }

    public async Task<UpdateOrderShippingResultAppDto> UpdateShippingAsync(UpdateOrderShippingAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var validateResult = dto.Validate();
        if (!validateResult.valid)
        {
            return new UpdateOrderShippingResultAppDto
            {
                Success = false,
                ErrorMessage = validateResult.errorMessage
            };
        }

        var order = await orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new UpdateOrderShippingResultAppDto
            {
                Success = false,
                ErrorMessage = "Order is not found."
            };
        }

        if (!order.CanUpdateInfo)
        {
            return new UpdateOrderShippingResultAppDto
            {
                Success = false,
                ErrorMessage = "Order cannot be change shipping status."
            };
        }

        await orderManager.UpdateShippingAsync(new UpdateShippingDto
        {
            OrderId = dto.OrderId,
            ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc,
            Address = dto.Address
        }).ConfigureAwait(false);

        return new UpdateOrderShippingResultAppDto
        {
            Success = true
        };
    }

    public async Task<LockOrderResultAppDto> LockOrderAsync(LockOrderAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new LockOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Order is not found."
            };
        }

        if (!order.CanUpdateInfo)
        {
            return new LockOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Order cannot be change shipping status."
            };
        }

        await orderManager.LockOrderAsync(new LockOrderDto
        {
            OrderId = dto.OrderId,
            Reason = dto.Reason
        }).ConfigureAwait(false);

        return new LockOrderResultAppDto
        {
            Success = true
        };
    }

    public async Task<OrderAppDto?> GetOrderByIdAsync(Guid id)
    {
        var order = await orderManager.GetOrderByIdAsync(id).ConfigureAwait(false);

        if (order is null)
            return null;

        return order.ToDto();
    }

    public async Task<IPagedDataAppDto<OrderAppDto>> GetOrdersAsync(string? keywords, int? status, int pageIndex, int pageSize)
    {
        OrderStatus? orderStatus = status.HasValue ? (OrderStatus)status : null;
        var pagedData = await orderManager.GetOrdersAsync(keywords, orderStatus, pageIndex, pageSize).ConfigureAwait(false);

        return PagedDataAppDto.Create(pagedData.Select(order => order.ToDto()), pageIndex, pageSize, pagedData.PagerInfo.TotalCount);
    }

    public async Task<CreateOrderResultAppDto> CreateOrderAsync(CreateOrderAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new CreateOrderResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var customer = await customerDataReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer is null)
        {
            return new CreateOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Customer is not found."
            };
        }

        if (dto.CreatedByUserId.HasValue)
        {
            var user = await userDataReader.GetByIdAsync(dto.CreatedByUserId.Value).ConfigureAwait(false);
            if (user is null)
            {
                return new CreateOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Created user is not found."
                };
            }
        }

        var code = await NextOrderCodeAsync().ConfigureAwait(false);
        var createOrderDto = new CreateOrderDto
        {
            Code = code,
            CustomerId = dto.CustomerId,
            Note = dto.Note,
            OrderDiscount = dto.OrderDiscount,
            CreatedByUserId = dto.CreatedByUserId,
            ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc,
            ShippingAddress = dto.ShippingAddress
        };
        foreach (var orderItem in dto.Items)
        {
            createOrderDto.Items.Add(new AddOrderItemDto
            {
                ProductId = orderItem.ProductId,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice
            });
        }

        var createOrderResult = await orderManager.CreateOrderAsync(createOrderDto).ConfigureAwait(false);

        return new CreateOrderResultAppDto
        {
            Success = true,
            CreatedId = createOrderResult.CreatedId
        };
    }

    public async Task<MarkOrderItemDeliveredResultAppDto> MarkOrderItemDeliveredAsync(MarkOrderItemDeliveredAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new MarkOrderItemDeliveredResultAppDto
            {
                Success = false,
                ErrorMessage = "Không tìm thấy đơn hàng."
            };
        }

        var orderItem = order.Items.FirstOrDefault(item => item.Id == dto.OrderItemId);
        if (orderItem is null)
        {
            return new MarkOrderItemDeliveredResultAppDto
            {
                Success = false,
                ErrorMessage = "Không tìm thấy hàng hóa trong đơn."
            };
        }

        if (orderItem.IsDelivered)
        {
            return new MarkOrderItemDeliveredResultAppDto
            {
                Success = false,
                ErrorMessage = "Hàng hóa đã được đánh dấu giao rồi."
            };
        }

        await orderManager.MarkOrderItemDeliveredAsync(new Domain.Shared.Dtos.Orders.MarkOrderItemDeliveredDto
        {
            OrderId = dto.OrderId,
            OrderItemId = dto.OrderItemId,
            PictureId = dto.PictureId
        }).ConfigureAwait(false);

        // Re-fetch to check if order was auto-locked
        var updatedOrder = await orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);

        return new MarkOrderItemDeliveredResultAppDto
        {
            Success = true,
            OrderAutoLocked = updatedOrder?.Status == OrderStatus.Locked
        };
    }

    public async Task<string> NextOrderCodeAsync()
    {
        var now = DateTime.UtcNow;
        var code = string.Empty;
        do
        {
            code = $"O-{now:yyyyMM}-{Random.Shared.Next(1000, 9999)}";
        }
        while (await orderManager.DoesCodeExistAsync(code).ConfigureAwait(false));

        return code;
    }
}

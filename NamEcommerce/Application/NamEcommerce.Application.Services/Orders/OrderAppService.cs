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

public sealed class OrderAppService : IOrderAppService
{
    private readonly IOrderManager _orderManager;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<Customer> _customerDataReader;
    private readonly IEntityDataReader<User> _userDataReader;

    public OrderAppService(IOrderManager orderManager, IEntityDataReader<Product> productDataReader, IEntityDataReader<Customer> customerDataReader, IEntityDataReader<User> userDataReader)
    {
        _orderManager = orderManager;
        _productDataReader = productDataReader;
        _customerDataReader = customerDataReader;
        _userDataReader = userDataReader;
    }

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

        var order = await _orderManager.GetOrderByIdAsync(dto.Id).ConfigureAwait(false);
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

        var updateResultDto = await _orderManager.UpdateOrderAsync(new UpdateOrderDto(dto.Id)
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

        var order = await _orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
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

        var product = await _productDataReader.GetByIdAsync(dto.ProductId).ConfigureAwait(false);
        if (product is null)
        {
            return new AddOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Product is not found."
            };
        }

        await _orderManager.AddOrderItemAsync(dto.OrderId, new AddOrderItemDto
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

        var order = await _orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
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

        await _orderManager.UpdateOrderItemAsync(new UpdateOrderItemDto
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

        var order = await _orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
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

        await _orderManager.DeleteOrderItemAsync(new DeleteOrderItemDto(dto.OrderId, dto.OrderItemId)).ConfigureAwait(false);

        return new DeleteOrderItemResultAppDto
        {
            Success = true
        };
    }

    public async Task<(bool success, string? errorMessage)> ChangeOrderStatusAsync(Guid orderId, int status)
    {
        var order = await _orderManager.GetOrderByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            return (false, "Order is not found.");

        if (!order.CanUpdateInfo)
            return (false, "Order status cannot changed.");

        var orderStatus = (OrderStatus)status;
        if (!Enum.IsDefined(orderStatus))
            return (false, "Status is invalid.");

        await _orderManager.ChangeOrderStatusAsync(orderId, orderStatus).ConfigureAwait(false);

        return (true, string.Empty);
    }

    public async Task<MarkOrderAsPaidResultAppDto> MarkAsPaidAsync(MarkOrderAsPaidAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await _orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new MarkOrderAsPaidResultAppDto
            {
                Success = false,
                ErrorMessage = "Order is not found."
            };
        }

        if (!order.CanUpdateInfo)
        {
            return new MarkOrderAsPaidResultAppDto
            {
                Success = false,
                ErrorMessage = "Order cannot be set to paid."
            };
        }

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            return new MarkOrderAsPaidResultAppDto
            {
                Success = false,
                ErrorMessage = "Order is already marked as paid."
            };
        }

        var paymentMethod = (PaymentMethod)dto.PaymentMethod;
        if (!Enum.IsDefined(paymentMethod))
        {
            return new MarkOrderAsPaidResultAppDto
            {
                Success = false,
                ErrorMessage = "Payment method is invalid."
            };
        }

        await _orderManager.MarkAsPaidAsync(new MarkAsPaidDto
        {
            OrderId = dto.OrderId,
            PaymentMethod = paymentMethod,
            Note = dto.Note
        }).ConfigureAwait(false);

        return new MarkOrderAsPaidResultAppDto
        {
            Success = true
        };
    }

    public async Task<UpdateOrderShippingResultAppDto> UpdateShippingAsync(UpdateOrderShippingAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await _orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
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

        if (order.ShippingStatus == ShippingStatus.Shipped)
        {
            return new UpdateOrderShippingResultAppDto
            {
                Success = false,
                ErrorMessage = "Order cannot be change shipping status."
            };
        }

        var shippingStatus = (ShippingStatus)dto.ShippingStatus;
        if (!Enum.IsDefined(shippingStatus))
        {
            return new UpdateOrderShippingResultAppDto
            {
                Success = false,
                ErrorMessage = "Payment method is invalid."
            };
        }

        await _orderManager.UpdateShippingAsync(new UpdateShippingDto
        {
            OrderId = dto.OrderId,
            ShippingStatus = shippingStatus,
            Address = dto.Address,
            Note = dto.Note
        }).ConfigureAwait(false);

        return new UpdateOrderShippingResultAppDto
        {
            Success = true
        };
    }

    public async Task<CancelOrderResultAppDto> CancelOrderAsync(CancelOrderAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await _orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new CancelOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Order is not found."
            };
        }

        if (!order.CanUpdateInfo)
        {
            return new CancelOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Order cannot be change shipping status."
            };
        }

        await _orderManager.CancelOrderAsync(new CancelOrderDto
        {
            OrderId = dto.OrderId,
            Reason = dto.Reason
        }).ConfigureAwait(false);

        return new CancelOrderResultAppDto
        {
            Success = true
        };
    }

    public async Task<OrderAppDto?> GetOrderByIdAsync(Guid id)
    {
        var order = await _orderManager.GetOrderByIdAsync(id).ConfigureAwait(false);

        if (order is null)
            return null;

        return order.ToDto();
    }

    public async Task<IPagedDataAppDto<OrderAppDto>> GetOrdersAsync(string? keywords, int? status, int pageIndex, int pageSize)
    {
        OrderStatus? orderStatus = status.HasValue ? (OrderStatus)status : null;
        var pagedData = await _orderManager.GetOrdersAsync(keywords, orderStatus, pageIndex, pageSize).ConfigureAwait(false);

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

        var customer = await _customerDataReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
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
            var user = await _userDataReader.GetByIdAsync(dto.CreatedByUserId.Value).ConfigureAwait(false);
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
            ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc
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

        var createOrderResult = await _orderManager.CreateOrderAsync(createOrderDto).ConfigureAwait(false);

        return new CreateOrderResultAppDto
        {
            Success = true,
            CreatedId = createOrderResult.CreatedId
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
        while (await _orderManager.DoesCodeExistAsync(code).ConfigureAwait(false));

        return code;
    }
}


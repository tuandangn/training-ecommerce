using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.Orders;

public sealed class OrderAppService(IOrderManager orderManager,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<Customer> customerDataReader,
    IEntityDataReader<DeliveryNote> deliveryNoteDataReader,
    IEntityDataReader<UnitMeasurement> unitMeasurementDataReader,
    IInventoryStockManager inventoryStockManager,
    IDirectShipManager directShipManager,
    IOrderFulfillmentScheduleAppService orderFulfillmentScheduleAppService,
    IShortageQueryService shortageQueryService) : IOrderAppService
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
                ErrorMessage = "Error.OrderIsNotFound"
            };
        }

        if (!order.CanUpdateInfo)
        {
            return new UpdateOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderCannotUpdateInfo"
            };
        }

        if ((dto.OrderDiscount ?? 0) > order.OrderSubTotal)
        {
            return new UpdateOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderDiscountExceedsTotal"
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
                ErrorMessage = "Error.OrderIsNotFound"
            };
        }

        if (!order.CanUpdateOrderItems)
        {
            return new AddOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderCannotUpdateOrderItems"
            };
        }

        var product = await productDataReader.GetByIdAsync(dto.ProductId, default).ConfigureAwait(false);
        if (product is null)
        {
            return new AddOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.ProductIsNotFound"
            };
        }

        if (product.UnitMeasurementId.HasValue)
        {
            var unitMeasurement = await unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value, default).ConfigureAwait(false);
            if (unitMeasurement is not null)
            {
                if (!NumberHelper.IsValidDecimalPlace(dto.Quantity, unitMeasurement.DecimalPlaces))
                {
                    return new AddOrderItemResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Error.QuantityMustBeInteger"
                    };
                }
            }
        }

        if (!product.ProductVendors.Any())
        {
            var availableQuantity = await inventoryStockManager.GetGlobalAvailableQuantityForProductAsync(dto.ProductId).ConfigureAwait(false);
            if (availableQuantity < dto.Quantity)
            {
                return new AddOrderItemResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.ProductInsufficientStock"
                };
            }
        }

        var existingItemIds = order.Items.Select(item => item.Id).ToHashSet();
        await orderManager.AddOrderItemAsync(dto.OrderId, new AddOrderItemDto
        {
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice
        }).ConfigureAwait(false);

        var updatedOrder = await orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
        var defaultScheduleResult = await CreateDefaultSchedulesAsync(
            updatedOrder,
            updatedOrder?.Items.Where(item => !existingItemIds.Contains(item.Id)).Select(item => item.Id).ToHashSet()).ConfigureAwait(false);
        if (!defaultScheduleResult.Success)
        {
            return new AddOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = defaultScheduleResult.ErrorMessage,
                OrderId = order.Id
            };
        }

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
                ErrorMessage = "Error.OrderIsNotFound"
            };
        }

        if (!order.CanUpdateOrderItems)
        {
            return new UpdateOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderCannotUpdateOrderItems"
            };
        }

        var orderItem = order.Items.FirstOrDefault(item => item.Id == dto.OrderItemId);
        if (orderItem is null)
        {
            return new UpdateOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderItemIsNotFound"
            };
        }

        var calculatedOrderSubTotal = order.OrderSubTotal - orderItem.SubTotal + dto.Quantity * dto.UnitPrice;
        if ((order.OrderDiscount ?? 0) > calculatedOrderSubTotal)
        {
            return new UpdateOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderDiscountExceedsTotal"
            };
        }


        var product = await productDataReader.GetByIdAsync(orderItem.ProductId, default).ConfigureAwait(false);
        if (product is null)
        {
            return new UpdateOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.ProductIsNotFound"
            };
        }

        if (product.UnitMeasurementId.HasValue)
        {
            var unitMeasurement = await unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value, default).ConfigureAwait(false);
            if (unitMeasurement is not null)
            {
                if (!NumberHelper.IsValidDecimalPlace(dto.Quantity, unitMeasurement.DecimalPlaces))
                {
                    return new UpdateOrderItemResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Error.QuantityMustBeInteger"
                    };
                }
            }
        }

        if (!product.ProductVendors.Any())
        {
            var needCheckQuantity = Math.Max(0, dto.Quantity - orderItem.Quantity);
            var availableQuantity = await inventoryStockManager.GetGlobalAvailableQuantityForProductAsync(orderItem.ProductId).ConfigureAwait(false);
            if (availableQuantity < dto.Quantity)
            {
                return new UpdateOrderItemResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.ProductInsufficientStock"
                };
            }
        }

        var deliveryNoteOrderItems = (from deliveryNote in deliveryNoteDataReader.DataSource
                                      where deliveryNote.OrderId == order.Id && deliveryNote.Items.Any(item => item.OrderItemId == dto.OrderItemId)
                                         && deliveryNote.Status != DeliveryNoteStatus.Cancelled
                                      select deliveryNote)
                                     .SelectMany(deliveryNote => deliveryNote.Items.Where(item => item.OrderItemId == dto.OrderItemId))
                                     .ToList();
        var deliveryNoteQty = deliveryNoteOrderItems.Sum(item => item.Quantity);
        if (dto.Quantity < deliveryNoteQty)
        {
            return new UpdateOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderItemQuantityInvalid_Delivering"
            };
        }
        if (deliveryNoteOrderItems.Any(item => item.UnitPrice != dto.UnitPrice))
        {
            return new UpdateOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderItemUnitPriceCannotChange_InDelivery"
            };
        }

        var scheduleQuantityResult = await EnsureActiveScheduleQuantityFitsAsync(dto.OrderId, dto.OrderItemId, dto.Quantity).ConfigureAwait(false);
        if (!scheduleQuantityResult.Success)
        {
            return new UpdateOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = scheduleQuantityResult.ErrorMessage,
                OrderId = dto.OrderId
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
                ErrorMessage = "Error.OrderIsNotFound"
            };
        }

        if (!order.CanUpdateOrderItems)
        {
            return new DeleteOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderCannotUpdateOrderItems"
            };
        }

        var orderItem = order.Items.FirstOrDefault(item => item.Id == dto.OrderItemId);
        if (orderItem is null)
        {
            return new DeleteOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderItemIsNotFound"
            };
        }

        var calculatedOrderSubTotal = order.OrderSubTotal - orderItem.SubTotal;
        if ((order.OrderDiscount ?? 0) > calculatedOrderSubTotal)
        {
            return new DeleteOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderDiscountExceedsTotal"
            };
        }

        var orderItemDeliveryNotes = from deliveryNote in deliveryNoteDataReader.DataSource
                                     where deliveryNote.OrderId == order.Id && deliveryNote.Items.Any(item => item.OrderItemId == dto.OrderItemId)
                                     select deliveryNote;
        if (orderItemDeliveryNotes.Any())
        {
            return new DeleteOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderItemCannotRemove_InDelivery"
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
                ErrorMessage = "Error.OrderIsNotFound"
            };
        }

        if (!order.CanUpdateInfo)
        {
            return new UpdateOrderShippingResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderCannotUpdateShipping"
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

    public async Task<CompleteOrderResultAppDto> CompleteOrderAsync(CompleteOrderAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new CompleteOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderIsNotFound"
            };
        }

        if (!order.CanCompleteOrder)
        {
            return new CompleteOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderCannotComplete"
            };
        }

        var hasDraftDn = deliveryNoteDataReader.DataSource.Any(dn =>
            dn.OrderId == dto.OrderId && dn.Status == DeliveryNoteStatus.Draft);
        if (hasDraftDn)
        {
            return new CompleteOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderHasDraftDeliveryNotes"
            };
        }

        try
        {
            await orderManager.CompleteOrderAsync(new CompleteOrderDto
            {
                OrderId = dto.OrderId
            }).ConfigureAwait(false);

            return new CompleteOrderResultAppDto
            {
                Success = true
            };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new CompleteOrderResultAppDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<OrderAppDto?> GetOrderByIdAsync(Guid id)
    {
        var order = await orderManager.GetOrderByIdAsync(id).ConfigureAwait(false);

        if (order is null)
            return null;

        return order.ToDto();
    }

    public async Task<IPagedDataAppDto<OrderAppDto>> GetOrdersAsync(int pageIndex, int pageSize, string? keywords, int? status)
    {
        OrderStatus? orderStatus = status.HasValue ? (OrderStatus)status : null;
        var pagedData = await orderManager.GetOrdersAsync(pageIndex, pageSize, keywords, orderStatus).ConfigureAwait(false);

        return PagedDataAppDto.Create(pagedData.Select(order => order.ToDto()), pageIndex, pageSize, pagedData.PagerInfo.TotalCount);
    }

    public async Task<IList<RecentSalePriceAppDto>> GetRecentSalePricesAsync(Guid productId, Guid customerId, int take = 10)
    {
        var domainDtos = await orderManager.GetRecentSalePricesAsync(productId, customerId, take).ConfigureAwait(false);

        return domainDtos
            .Select(d => new RecentSalePriceAppDto(
                CustomerId: d.CustomerId,
                CustomerName: d.CustomerName,
                UnitPrice: d.UnitPrice,
                OrderCode: d.OrderCode,
                OrderDate: d.OrderDateUtc.ToLocalTime()))
            .ToList();
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

        var customer = await customerDataReader.GetByIdAsync(dto.CustomerId, default).ConfigureAwait(false);
        if (customer is null)
        {
            return new CreateOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.CustomerIsNotFound"
            };
        }

        foreach (var itemGroup in dto.Items.GroupBy(item => item.ProductId))
        {
            var product = await productDataReader.GetByIdAsync(itemGroup.Key, default).ConfigureAwait(false);
            if (product is null)
            {
                return new CreateOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.ProductIsNotFound"
                };
            }

            if (product.UnitMeasurementId.HasValue)
            {
                var unitMeasurement = await unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value, default).ConfigureAwait(false);
                if (unitMeasurement is not null)
                {
                    foreach (var item in itemGroup)
                    {
                        if (!NumberHelper.IsValidDecimalPlace(item.Quantity, unitMeasurement.DecimalPlaces))
                        {
                            return new CreateOrderResultAppDto
                            {
                                Success = false,
                                ErrorMessage = "Error.QuantityMustBeInteger"
                            };
                        }
                    }
                }
            }

            if (product.ProductVendors.Any())
                continue;

            var requestedQuantity = itemGroup.Sum(item => item.Quantity);
            var availableQuantity = await inventoryStockManager.GetGlobalAvailableQuantityForProductAsync(itemGroup.Key).ConfigureAwait(false);
            if (availableQuantity < requestedQuantity)
            {
                return new CreateOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.ProductInsufficientStock"
                };
            }
        }

        var createOrderDto = new CreateOrderDto
        {
            CustomerId = dto.CustomerId,
            Note = dto.Note,
            OrderDiscount = dto.OrderDiscount,
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
        var createdOrder = await orderManager.GetOrderByIdAsync(createOrderResult.CreatedId).ConfigureAwait(false);
        var defaultScheduleResult = await CreateDefaultSchedulesAsync(createdOrder, null).ConfigureAwait(false);
        if (!defaultScheduleResult.Success)
        {
            return new CreateOrderResultAppDto
            {
                Success = false,
                ErrorMessage = defaultScheduleResult.ErrorMessage,
                CreatedId = createOrderResult.CreatedId
            };
        }

        return new CreateOrderResultAppDto
        {
            Success = true,
            CreatedId = createOrderResult.CreatedId
        };
    }

    private async Task<CommonActionResultDto> EnsureActiveScheduleQuantityFitsAsync(Guid orderId, Guid orderItemId, decimal newQuantity)
    {
        var schedules = await orderFulfillmentScheduleAppService.GetByOrderIdAsync(orderId).ConfigureAwait(false);
        var activeScheduledQuantity = schedules
            .Where(schedule => schedule.IsActive)
            .SelectMany(schedule => schedule.Items)
            .Where(item => item.OrderItemId == orderItemId)
            .Sum(item => item.Quantity);
        if (activeScheduledQuantity == 0)
            return CommonActionResultDto.CreateSuccess();

        var states = await shortageQueryService.GetOrderItemFulfillmentStatesAsync(orderId).ConfigureAwait(false);
        var shippedQuantity = states.FirstOrDefault(state => state.OrderItemId == orderItemId)?.ShippedQuantity ?? 0;
        var newRemainingQuantity = Math.Max(0, newQuantity - shippedQuantity);

        return activeScheduledQuantity <= newRemainingQuantity
            ? CommonActionResultDto.CreateSuccess()
            : CommonActionResultDto.CreateError("Error.OrderFulfillmentScheduleQuantityExceedsOrderItemQuantity");
    }

    private async Task<CommonActionResultDto> CreateDefaultSchedulesAsync(OrderDto? order, ISet<Guid>? limitedOrderItemIds)
    {
        if (order is null)
            return CommonActionResultDto.CreateSuccess();

        var states = await shortageQueryService.GetOrderItemFulfillmentStatesAsync(order.Id).ConfigureAwait(false);
        if (limitedOrderItemIds is not null)
            states = states.Where(state => limitedOrderItemIds.Contains(state.OrderItemId)).ToList();

        if (states.Count == 0)
            return CommonActionResultDto.CreateSuccess();

        if (order.ExpectedShippingDateUtc.HasValue)
        {
            return await CreateScheduleAsync(order.Id, OrderFulfillmentScheduleMode.NotBeforeDate, order.ExpectedShippingDateUtc, states
                .Select(state => ToScheduleItem(state, Math.Max(0, state.RequiredQuantity - state.ShippedQuantity)))
                .Where(item => item.Quantity > 0)
                .ToList()).ConfigureAwait(false);
        }

        var asapItems = states
            .Select(state =>
            {
                var remaining = Math.Max(0, state.RequiredQuantity - state.ShippedQuantity);
                return ToScheduleItem(state, Math.Min(remaining, state.AvailableQuantity));
            })
            .Where(item => item.Quantity > 0)
            .ToList();
        var asapResult = await CreateScheduleAsync(order.Id, OrderFulfillmentScheduleMode.AsSoonAsPossible, null, asapItems).ConfigureAwait(false);
        if (!asapResult.Success)
            return asapResult;

        var waitingItems = states
            .Select(state =>
            {
                var remaining = Math.Max(0, state.RequiredQuantity - state.ShippedQuantity);
                var available = Math.Min(remaining, state.AvailableQuantity);
                return ToScheduleItem(state, remaining - available);
            })
            .Where(item => item.Quantity > 0)
            .ToList();

        return await CreateScheduleAsync(order.Id, OrderFulfillmentScheduleMode.WhenStockAvailable, null, waitingItems).ConfigureAwait(false);
    }

    private async Task<CommonActionResultDto> CreateScheduleAsync(
        Guid orderId,
        OrderFulfillmentScheduleMode mode,
        DateTime? scheduledFromUtc,
        IList<OrderFulfillmentScheduleItemInputAppDto> items)
    {
        if (items.Count == 0)
            return CommonActionResultDto.CreateSuccess();

        var result = await orderFulfillmentScheduleAppService.CreateAsync(new CreateOrderFulfillmentScheduleAppDto
        {
            OrderId = orderId,
            Mode = (int)mode,
            ScheduledFromUtc = scheduledFromUtc,
            Items = items
        }).ConfigureAwait(false);

        return result.Success
            ? CommonActionResultDto.CreateSuccess()
            : CommonActionResultDto.CreateError(result.ErrorMessage);
    }

    private static OrderFulfillmentScheduleItemInputAppDto ToScheduleItem(
        Domain.Shared.Dtos.Inventory.OrderItemFulfillmentStateDto state,
        decimal quantity)
        => new()
        {
            OrderItemId = state.OrderItemId,
            ProductId = state.ProductId,
            ProductName = state.ProductName,
            Quantity = quantity
        };

    public async Task<MarkOrderItemDeliveredResultAppDto> MarkOrderItemDeliveredAsync(MarkOrderItemDeliveredAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new MarkOrderItemDeliveredResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderIsNotFound"
            };
        }

        var orderItem = order.Items.FirstOrDefault(item => item.Id == dto.OrderItemId);
        if (orderItem is null)
        {
            return new MarkOrderItemDeliveredResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderItemIsNotFound"
            };
        }

        if (orderItem.IsDelivered)
        {
            return new MarkOrderItemDeliveredResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderItemAlreadyDelivered"
            };
        }

        await orderManager.MarkOrderItemDeliveredAsync(new Domain.Shared.Dtos.Orders.MarkOrderItemDeliveredDto
        {
            OrderId = dto.OrderId,
            OrderItemId = dto.OrderItemId,
            PictureId = dto.PictureId
        }).ConfigureAwait(false);

        return new MarkOrderItemDeliveredResultAppDto
        {
            Success = true
        };
    }

    public async Task<DeleteOrderResultAppDto> DeleteOrderAsync(DeleteOrderAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = await orderManager.GetOrderByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new DeleteOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderIsNotFound"
            };
        }

        var canDeleteOrder = order.Status is OrderStatus.Pending or OrderStatus.Cancelled;
        if (!canDeleteOrder)
        {
            return new DeleteOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderCannotDelete"
            };
        }

        var activeDeliveryNotes = from deliveryNote in deliveryNoteDataReader.DataSource
                                  where deliveryNote.OrderId == order.Id && deliveryNote.Status != DeliveryNoteStatus.Cancelled
                                  select deliveryNote;
        if (activeDeliveryNotes.Any())
        {
            return new DeleteOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderCannotDelete_Processing"
            };
        }

        await orderManager.DeleteOrderAsync(new DeleteOrderDto(order.Id)).ConfigureAwait(false);

        return new DeleteOrderResultAppDto
        {
            Success = true
        };
    }

    public async Task<CancelOrderResultAppDto> CancelOrderAsync(CancelOrderAppDto dto)
    {
        try
        {
            var hasBlockingDeliveryNotes = deliveryNoteDataReader.DataSource
                .Any(d => d.OrderId == dto.OrderId
                    && d.Status != DeliveryNoteStatus.Cancelled
                    && (d.SourceType != DeliveryNoteSourceType.DirectShipToCustomer
                        || d.Status != DeliveryNoteStatus.Confirmed));
            if (hasBlockingDeliveryNotes)
                return new CancelOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.OrderCannotCancel_Processing"
                };

            var hasReceivedDirectShipAllocations = await directShipManager
                .HasReceivedDirectShipAllocationsAsync(dto.OrderId)
                .ConfigureAwait(false);
            if (hasReceivedDirectShipAllocations)
            {
                if (!dto.ReturnWarehouseId.HasValue || dto.ReturnWarehouseId == Guid.Empty)
                    return new CancelOrderResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Vui lòng chọn kho nhận hàng trả về."
                    };

                await directShipManager.HandleSoCancelledForReceivedDirectShipAsync(
                    dto.OrderId,
                    dto.ReturnWarehouseId.Value,
                    Guid.Empty,
                    $"Đơn bán {dto.OrderId} bị hủy — chuyển hàng giao thẳng về kho đã chọn").ConfigureAwait(false);
            }

            await orderManager.CancelOrderAsync(new CancelOrderDto(dto.OrderId)
            {
                FullyReceivedAllocationIds = []
            }).ConfigureAwait(false);

            return new CancelOrderResultAppDto { Success = true };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new CancelOrderResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }
}

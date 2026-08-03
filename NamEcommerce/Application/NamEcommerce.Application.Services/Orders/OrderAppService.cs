using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;
using NamEcommerce.Domain.Specifications.DeliveryNotes;

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

        var product = await productDataReader.GetByIdAsync(dto.ProductId).ConfigureAwait(false);
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
            var unitMeasurement = await unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value).ConfigureAwait(false);
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
        var addedItemIds = updatedOrder?.Items.Where(item => !existingItemIds.Contains(item.Id)).Select(item => item.Id).Distinct().ToList();
        var defaultScheduleResult = await orderFulfillmentScheduleAppService.CreateDefaultSchedulesForOrderAsync(updatedOrder!.Id, addedItemIds).ConfigureAwait(false);
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

        var calculatedOrderTotal = order.TotalAmount - orderItem.SubTotal + dto.Quantity * dto.UnitPrice;
        if (calculatedOrderTotal < order.PaidAmount)
        {
            return new UpdateOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.AfterUpdateOrderItemTotalCannotBeLessThanPaidAmount"
            };
        }

        var product = await productDataReader.GetByIdAsync(orderItem.ProductId).ConfigureAwait(false);
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
            var unitMeasurement = await unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value).ConfigureAwait(false);
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

        if (product.ProductVendors.Count == 0)
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

        var activeDeliveryNotes = await deliveryNoteDataReader.GetListAsync(new ActiveDeliveryNotesOfOrderItemsSpec(dto.OrderId, [dto.OrderItemId])).ConfigureAwait(false);
        var deliveryNoteOrderItems = activeDeliveryNotes.SelectMany(deliveryNote
            => deliveryNote.Items.Where(item => item.OrderItemId == dto.OrderItemId))
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

        var calculatedOrderTotal = order.TotalAmount - orderItem.SubTotal;
        if (calculatedOrderTotal < order.PaidAmount)
        {
            return new DeleteOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.AfterDeletingOrderItemTotalCannotBeLessThanPaidAmount"
            };
        }

        var hasDeliveryNotes = await deliveryNoteDataReader.AnyAsync(new ActiveDeliveryNotesOfOrderItemsSpec(dto.OrderId, [dto.OrderItemId])).ConfigureAwait(false);
        if (hasDeliveryNotes)
        {
            return new DeleteOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderItemCannotRemove_InDelivery"
            };
        }

        await orderFulfillmentScheduleAppService.DeleteScheduleItemsOfOrderItemsAsync(dto.OrderId, [dto.OrderItemId]).ConfigureAwait(false);
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
            Address = dto.Address,
            PhoneNumber = dto.PhoneNumber
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

    public async Task<IPagedDataAppDto<OrderAppDto>> GetOrdersAsync(int pageIndex, int pageSize, string? keywords = null, int? status = null, bool? isPaymentRequired = null)
    {
        OrderStatus? orderStatus = status.HasValue ? (OrderStatus)status : null;
        var pagedData = await orderManager.GetOrdersAsync(pageIndex, pageSize, keywords, orderStatus, null, isPaymentRequired).ConfigureAwait(false);

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
                OrderDateUtc: d.OrderDateUtc))
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

        var customer = await customerDataReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
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
            var product = await productDataReader.GetByIdAsync(itemGroup.Key).ConfigureAwait(false);
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
                var unitMeasurement = await unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value).ConfigureAwait(false);
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
            ShippingAddress = dto.ShippingAddress,
            ShippingPhoneNumber = dto.ShippingPhoneNumber,
            RequiresPayOff = dto.RequiresPayOff
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
        if (dto.SkipScheduling)
        {
            return new CreateOrderResultAppDto
            {
                Success = true,
                CreatedId = createOrderResult.CreatedId
            };
        }

        var defaultScheduleResult = await orderFulfillmentScheduleAppService.CreateDefaultSchedulesForOrderAsync(createOrderResult.CreatedId, null).ConfigureAwait(false);
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

    public async Task<decimal> GetRemainShippingQuantityForOrderItemAsync(Guid orderId, Guid orderItemId)
    {
        var order = await orderManager.GetOrderByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);

        var orderItem = order.Items.FirstOrDefault(item => item.Id == orderItemId);
        if (orderItem is null)
            throw new OrderItemIsNotFoundException();

        var states = await shortageQueryService.GetOrderItemFulfillmentStatesAsync(order.Id).ConfigureAwait(false);
        var shippedQuantity = states.FirstOrDefault(state => state.OrderItemId == orderItem.Id)?.ShippedQuantity ?? 0;

        var newRemainingQuantity = Math.Max(0, orderItem.Quantity - shippedQuantity);
        return newRemainingQuantity;
    }
}

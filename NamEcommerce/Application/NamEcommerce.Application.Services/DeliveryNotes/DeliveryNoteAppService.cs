using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.Localizations;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Application.Services.Notifications;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Specifications.Inventory;

namespace NamEcommerce.Application.Services.DeliveryNotes;

public sealed class DeliveryNoteAppService(
    IDeliveryNoteManager deliveryNoteManager,
    IWarehouseAppService warehouseAppService,
    IUserAppService userAppService,
    ISystemNotificationAppService systemNotificationAppService,
    IEntityDataReader<Order> orderDataReader,
    IEntityDataReader<ProductReservationLedger> productReservationDataReader,
    IEntityDataReader<InventoryStock> inventoryStockDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<UnitMeasurement> unitMeasurementDataReader,
    ILocalizationAppService localizationAppService) : IDeliveryNoteAppService
{
    public async Task<CreateDeliveryNoteResultAppDto> CreateFromOrderAsync(CreateDeliveryNoteAppDto dto)
    {
        var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new CreateDeliveryNoteResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.OrderIsNotFound"
            };
        }

        var warehouse = await warehouseAppService.GetWarehouseByIdAsync(dto.WarehouseId).ConfigureAwait(false);
        if (warehouse is null)
        {
            return new CreateDeliveryNoteResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.WarehouseIsNotFound"
            };
        }

        foreach (var warehouseId in dto.Items.Select(item => item.WarehouseId).Distinct())
        {
            if (warehouseId == dto.WarehouseId)
                continue;

            if (await warehouseAppService.GetWarehouseByIdAsync(warehouseId).ConfigureAwait(false) is null)
            {
                if (warehouse is null)
                {
                    return new CreateDeliveryNoteResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Error.WarehouseIsNotFound"
                    };
                }
            }
        }

        foreach (var item in dto.Items)
        {
            var orderItem = order.OrderItems.FirstOrDefault(orderItem => orderItem.Id == item.OrderItemId);
            if (orderItem is null)
            {
                return new CreateDeliveryNoteResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.OrderItemIsNotFound"
                };
            }
            var product = await productDataReader.GetByIdAsync(orderItem.ProductId).ConfigureAwait(false);
            if (product is null)
            {
                return new CreateDeliveryNoteResultAppDto
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
                    if (!NumberHelper.IsValidDecimalPlace(item.Quantity, unitMeasurement.DecimalPlaces))
                    {
                        return new CreateDeliveryNoteResultAppDto
                        {
                            Success = false,
                            ErrorMessage = "Error.QuantityMustBeInteger"
                        };
                    }
                }
            }
        }

        var orderItemsById = order.OrderItems.ToDictionary(item => item.Id);
        var itemsByProduct = dto.Items
            .GroupBy(item => orderItemsById[item.OrderItemId].ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(item => item.Quantity) })
            .ToList();
        foreach (var item in itemsByProduct)
        {
            var reservedQuantity = (await productReservationDataReader.GetListAsync(new ProductReservationsOfOrderSpec(item.ProductId, order.Id)).ConfigureAwait(false))
                .Sum(reservation => reservation.QuantityDelta);
            if (reservedQuantity < item.Quantity)
            {
                return new CreateDeliveryNoteResultAppDto
                {
                    Success = false,
                    ErrorMessage = localizationAppService.GetValue("Error.CannotReleaseMoreThanReserved", [reservedQuantity, item.Quantity])
                };
            }
        }

        var itemsByProductWarehouse = dto.Items
            .GroupBy(item => new { orderItemsById[item.OrderItemId].ProductId, item.WarehouseId })
            .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, Quantity = g.Sum(item => item.Quantity) })
            .ToList();
        foreach (var item in itemsByProductWarehouse)
        {
            var stock = await inventoryStockDataReader.FirstOrDefaultAsync(new ProductInventoryStockSpec(item.ProductId, item.WarehouseId)).ConfigureAwait(false);
            if (stock is null)
            {
                return new CreateDeliveryNoteResultAppDto
                {
                    Success = false,
                    ErrorMessage = localizationAppService.GetValue("Error.InsufficientStock", [item.ProductId, item.WarehouseId, item.Quantity, 0])
                };
            }
            if (stock.QuantityAvailable < item.Quantity)
            {
                return new CreateDeliveryNoteResultAppDto
                {
                    Success = false,
                    ErrorMessage = localizationAppService.GetValue("Error.InsufficientStock", [item.ProductId, item.WarehouseId, item.Quantity, stock.QuantityAvailable])
                };
            }
        }

        var domainDto = new CreateDeliveryNoteDto
        {
            OrderId = dto.OrderId,
            ShippingAddress = dto.ShippingAddress,
            ShowPrice = dto.ShowPrice,
            CompensateReturnedQuantityInNextDelivery = dto.CompensateReturnedQuantityInNextDelivery,
            Note = dto.Note,
            WarehouseId = dto.WarehouseId,
            WarehouseName = warehouse.Name,
            Surcharge = dto.Surcharge,
            SurchargeReason = dto.SurchargeReason,
            AmountToCollect = dto.AmountToCollect,
            Items = dto.Items.Select(i => new CreateDeliveryNoteItemDto
            {
                OrderItemId = i.OrderItemId,
                WarehouseId = i.WarehouseId,
                Quantity = i.Quantity
            }).ToList()
        };
        var result = await deliveryNoteManager.CreateFromOrderAsync(domainDto).ConfigureAwait(false);

        return new CreateDeliveryNoteResultAppDto
        {
            Success = true,
            CreatedId = result.Id
        };
    }

    public async Task<CreateDeliveryNoteResultAppDto> CreateAsDeliveredFromVendorReturnAsync(CreateDeliveryNoteFromVendorReturnAppDto dto)
    {
        var domainDto = new CreateDeliveryNoteFromVendorReturnDto
        {
            VendorReturnId = dto.VendorReturnId,
            WarehouseId = dto.WarehouseId,
            Items = dto.Items.Select(i => new CreateDeliveryNoteFromVendorReturnItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            })
        };
        var createdId = await deliveryNoteManager.CreateAsDeliveredAsync(domainDto).ConfigureAwait(false);

        return new CreateDeliveryNoteResultAppDto
        {
            Success = true,
            CreatedId = createdId
        };
    }

    public async Task CancelAsync(Guid id)
    {
        await deliveryNoteManager.CancelAsync(id).ConfigureAwait(false);
    }

    public async Task ConfirmAsync(Guid id)
    {
        await deliveryNoteManager.ConfirmAsync(id).ConfigureAwait(false);
    }

    public async Task<CommonActionResultDto> MarkDeliveringAsync(Guid id)
    {
        try
        {
            await deliveryNoteManager.MarkDeliveringAsync(id).ConfigureAwait(false);
            return CommonActionResultDto.CreateSuccess();
        }
        catch (NamEcommerceDomainException ex)
        {
            return CommonActionResultDto.CreateError(ex.ErrorCode);
        }
    }

    public async Task<MarkDeliveryNoteDeliveredResultAppDto> MarkDeliveredAsync(MarkDeliveryNoteDeliveredAppDto dto)
    {
        try
        {
            var note = await deliveryNoteManager.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false);
            var isCompletedByAdminOnBehalf = note is not null
                && note.AssignedDeliveryUserId.HasValue
                && note.Status != DeliveryNoteStatus.Delivered
                && !string.Equals(dto.CompletionMetadata?.Source, "MobilePwa", StringComparison.OrdinalIgnoreCase);

            await deliveryNoteManager.MarkDeliveredAsync(new MarkDeliveryNoteDeliveredDto
            {
                DeliveryNoteId = dto.DeliveryNoteId,
                PictureIds = dto.PictureIds,
                ReceiverName = dto.ReceiverName,
                Acceptance = dto.Acceptance is null
                    ? null
                    : new DeliveryAcceptanceDto
                    {
                        AgreedCustomerCharge = dto.Acceptance.AgreedCustomerCharge,
                        AgreedCustomerChargeReason = dto.Acceptance.AgreedCustomerChargeReason,
                        CompensateInNextDelivery = dto.Acceptance.CompensateInNextDelivery,
                        Items = dto.Acceptance.Items.Select(item => new DeliveryAcceptanceItemDto
                        {
                            DeliveryNoteItemId = item.DeliveryNoteItemId,
                            AcceptedQuantity = item.AcceptedQuantity,
                            RejectedQuantity = item.RejectedQuantity,
                            RejectReason = item.RejectReason
                        }).ToList()
                    }
                ,
                CompletionMetadata = dto.CompletionMetadata is null
                    ? null
                    : new DeliveryCompletionMetadataDto
                    {
                        Latitude = dto.CompletionMetadata.Latitude,
                        Longitude = dto.CompletionMetadata.Longitude,
                        LocationAddress = dto.CompletionMetadata.LocationAddress,
                        Note = dto.CompletionMetadata.Note,
                        Source = dto.CompletionMetadata.Source,
                        IdempotencyKey = dto.CompletionMetadata.IdempotencyKey,
                        CashCollectedAmount = dto.CompletionMetadata.CashCollectedAmount
                    }
            }).ConfigureAwait(false);

            if (isCompletedByAdminOnBehalf)
                await systemNotificationAppService.CreateAsync(
                    DeliveryNoteSystemNotificationComposer.ShipperNotResponded(
                        note!.Id, note.Code, note.AssignedDeliveryFullName)).ConfigureAwait(false);

            return new MarkDeliveryNoteDeliveredResultAppDto
            {
                Success = true
            };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new MarkDeliveryNoteDeliveredResultAppDto
            {
                Success = false,
                ErrorMessage = ex.ErrorCode
            };
        }
        catch (Exception ex)
        {
            return new MarkDeliveryNoteDeliveredResultAppDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<AssignDeliveryUserResultAppDto> AssignDeliveryUserAsync(AssignDeliveryUserAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new AssignDeliveryUserResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var user = await userAppService.GetUserByIdAsync(dto.AssignedDeliveryUserId).ConfigureAwait(false);
        if (user is null)
        {
            return new AssignDeliveryUserResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.UserNotFound"
            };
        }

        var isDeliveryStaff = await userAppService
            .IsUserInRoleAsync(user.Id, SystemUserRoleNames.DeliveryStaff)
            .ConfigureAwait(false);
        if (!isDeliveryStaff)
        {
            return new AssignDeliveryUserResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.UserIsNotDeliveryStaff"
            };
        }

        try
        {
            await deliveryNoteManager.AssignDeliveryUserAsync(new AssignDeliveryUserDto
            {
                DeliveryNoteId = dto.DeliveryNoteId,
                AssignedDeliveryUserId = user.Id,
                AssignedDeliveryUsername = user.Username,
                AssignedDeliveryFullName = user.FullName
            }).ConfigureAwait(false);

            return new AssignDeliveryUserResultAppDto
            {
                Success = true
            };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new AssignDeliveryUserResultAppDto
            {
                Success = false,
                ErrorMessage = ex.ErrorCode
            };
        }
    }

    public async Task<DeliveryNoteAppDto?> GetByIdAsync(Guid id)
    {
        var result = await deliveryNoteManager.GetByIdAsync(id).ConfigureAwait(false);
        return result?.ToDto();
    }

    public async Task<IList<DeliveryNoteAppDto>> GetByOrderIdAsync(Guid orderId)
    {
        // For simplicity, we use GetListAsync and filter it down.
        // In a real app we might add a specific domain query.
        // Assuming page 0 to MAX gets enough for a single order's notes.
        var paged = await deliveryNoteManager.GetDeliveryNotesAsync(0, int.MaxValue).ConfigureAwait(false);
        return paged.Where(d => d.OrderId == orderId)
                    .Select(d => d.ToDto())
                    .OrderBy(d => d.CreatedOnUtc)
                    .ToList();
    }

    public async Task<PagedDataAppDto<DeliveryNoteAppDto>> GetListAsync(string? keywords = null, int pageIndex = 0, int pageSize = 15)
    {
        var paged = await deliveryNoteManager.GetDeliveryNotesAsync(pageIndex, pageSize, keywords).ConfigureAwait(false);
        var mappedItems = paged.Items.Select(d => d.ToDto()).ToList();
        var result = PagedDataAppDto.Create(mappedItems, paged.PagerInfo.PageIndex, paged.PagerInfo.PageSize, paged.PagerInfo.TotalCount);
        return (PagedDataAppDto<DeliveryNoteAppDto>)result;
    }
}

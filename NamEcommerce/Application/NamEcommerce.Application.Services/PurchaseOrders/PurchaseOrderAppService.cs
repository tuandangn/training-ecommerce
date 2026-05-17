using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.PurchaseOrders;

public sealed class PurchaseOrderAppService : IPurchaseOrderAppService
{
    private readonly IPurchaseOrderManager _purchaseOrderManager;
    private readonly IPurchaseOrderAllocationManager _purchaseOrderAllocationManager;
    private readonly IDirectShipManager? _directShipManager;
    private readonly IEntityDataReader<Vendor> _vendorDataReader;
    private readonly IEntityDataReader<Warehouse> _warehouseDataReader;
    private readonly IEntityDataReader<User> _userDataReader;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<PurchaseOrder> _purchaseOrderDataReader;

    public PurchaseOrderAppService(IPurchaseOrderManager purchaseOrderManager,
        IPurchaseOrderAllocationManager purchaseOrderAllocationManager,
        IEntityDataReader<PurchaseOrder> purchaseOrderDataReader, IEntityDataReader<Vendor> vendorDataReader,
        IEntityDataReader<Warehouse> warehouseDataReader, IEntityDataReader<User> userDataReader, IEntityDataReader<Product> productDataReader,
        IDirectShipManager? directShipManager = null)
    {
        _purchaseOrderManager = purchaseOrderManager;
        _purchaseOrderAllocationManager = purchaseOrderAllocationManager;
        _purchaseOrderDataReader = purchaseOrderDataReader;
        _vendorDataReader = vendorDataReader;
        _warehouseDataReader = warehouseDataReader;
        _userDataReader = userDataReader;
        _productDataReader = productDataReader;
        _directShipManager = directShipManager;
    }

    public async Task<IPagedDataAppDto<PurchaseOrderAppDto>> GetPurchaseOrdersAsync(int pageIndex, int pageSize, string? keywords, int? status)
    {
        PurchaseOrderStatus? poStatus = status.HasValue ? (PurchaseOrderStatus)status.Value : null;
        var pagedData = await _purchaseOrderManager.GetPurchaseOrdersAsync(pageIndex, pageSize, keywords, poStatus).ConfigureAwait(false);

        return PagedDataAppDto.Create(pagedData.Items.Select(item => item.ToDto()), pageIndex, pageSize, pagedData.PagerInfo.TotalCount);
    }

    public async Task<PurchaseOrderAppDto?> GetPurchaseOrderByIdAsync(Guid id)
    {
        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(id).ConfigureAwait(false);
        if (purchaseOrder is null)
            return null;

        return purchaseOrder.ToDto();
    }

    public async Task<PurchaseOrderAppDto?> GetPurchaseOrderByCodeAsync(string code)
    {
        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByCodeAsync(code).ConfigureAwait(false);
        if (purchaseOrder is null)
            return null;

        return purchaseOrder.ToDto();
    }

    public async Task<CreatePurchaseOrderResultAppDto> CreatePurchaseOrderAsync(CreatePurchaseOrderAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new CreatePurchaseOrderResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        if (dto.ExpectedDeliveryDateUtc < DateTime.UtcNow)
            throw new PurchaseOrderDataIsInvalidException("Error.ExpectedDeliveryDateCannotBeInPast");

        var vendor = await _vendorDataReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor is null)
        {
            return new CreatePurchaseOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.VendorIsNotFound"
            };
        }

        var products = await _productDataReader.GetByIdsAsync(dto.Items.Select(item => item.ProductId).OfType<Guid>()).ConfigureAwait(false);
        var candidateVendorIds = products.SelectMany(p => p.ProductVendors).Select(v => v.VendorId).Distinct().ToList();
        var validVendorIds = candidateVendorIds.Where(vendorId => products.All(p => p.ProductVendors.Any(v => v.VendorId == vendorId))).ToList();
        if (validVendorIds.Count == 0)
        {
            return new CreatePurchaseOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.PurchaseOrder.NoVendorsAppropriate"
            };
        }

        if (!validVendorIds.Contains(dto.VendorId))
        {
            return new CreatePurchaseOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.PurchaseOrder.VendorIsNotAppropriate"
            };
        }

        if (dto.WarehouseId.HasValue)
        {
            var warehouse = await _warehouseDataReader.GetByIdAsync(dto.WarehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
            {
                return new CreatePurchaseOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.WarehouseIsNotFound"
                };
            }
        }

        var createPurchaseOrderDto = new CreatePurchaseOrderDto
        {
            PlacedOnUtc = dto.PlacedOnUtc,
            VendorId = dto.VendorId,
            WarehouseId = dto.WarehouseId,
            ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc,
            Note = dto.Note,
            TaxAmount = dto.TaxAmount,
            ShippingAmount = dto.ShippingAmount
        };
        foreach (var item in dto.Items)
        {
            createPurchaseOrderDto.Items.Add(new PurchaseOrderItemDto(Guid.NewGuid())
            {
                PurchaseOrderId = Guid.Empty, // Will be set by manager
                ProductId = item.ProductId ?? Guid.Empty,
                QuantityOrdered = item.Quantity,
                UnitCost = item.UnitCost
            });
        }
        var result = await _purchaseOrderManager.CreatePurchaseOrderAsync(createPurchaseOrderDto).ConfigureAwait(false);

        return new CreatePurchaseOrderResultAppDto
        {
            Success = true,
            CreatedId = result.CreatedId
        };
    }

    public async Task<UpdatePurchaseOrderResultAppDto> UpdatePurchaseOrderAsync(UpdatePurchaseOrderAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new UpdatePurchaseOrderResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.Id).ConfigureAwait(false);
        if (purchaseOrder is null)
        {
            return new UpdatePurchaseOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.PurchaseOrderIsNotFound"
            };
        }

        var canModifyInfo = purchaseOrder.Status != PurchaseOrderStatus.Submitted
            && purchaseOrder.Status != PurchaseOrderStatus.Completed
            && purchaseOrder.Status != PurchaseOrderStatus.Cancelled;
        var canChangeVendor = purchaseOrder.Status == PurchaseOrderStatus.Draft;
        var canChangeDate = purchaseOrder.Status == PurchaseOrderStatus.Draft;
        var canChangeFees = purchaseOrder.Items.Count > 0 && purchaseOrder.Status == PurchaseOrderStatus.Receiving;

        if (!canModifyInfo)
        {
            return new UpdatePurchaseOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.PurchaseOrderCannotUpdateInfo"
            };
        }

        if (dto.ExpectedDeliveryDateUtc < DateTime.UtcNow && dto.ExpectedDeliveryDateUtc != purchaseOrder.ExpectedDeliveryDateUtc)
        {
            return new UpdatePurchaseOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.ExpectedDeliveryDateCannotBeInPast"
            };
        }

        if (canChangeVendor)
        {
            if (dto.VendorId != purchaseOrder.VendorId)
            {
                return new UpdatePurchaseOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.PurchaseOrderCannotUpdateVendor"
                };
            }
            else
            {
                var vendor = await _vendorDataReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
                if (vendor is null)
                {
                    return new UpdatePurchaseOrderResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Error.VendorIsNotFound"
                    };
                }
            }
        }

        if (canChangeFees)
        {
            if (purchaseOrder.Items.Count == 0)
            {
                if (dto.ShippingAmount > 0)
                {
                    return new UpdatePurchaseOrderResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Error.PurchaseOrderHasNoItemsForShipping"
                    };
                }
                if (dto.TaxAmount > 0)
                {
                    return new UpdatePurchaseOrderResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Error.PurchaseOrderHasNoItemsForTax"
                    };
                }
            }
            else
            {
                if (dto.TaxAmount < 0)
                {
                    return new UpdatePurchaseOrderResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Error.TaxAmountCannotBeNegative"
                    };
                }
                if (dto.ShippingAmount < 0)
                {
                    return new UpdatePurchaseOrderResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Error.ShippingAmountCannotBeNegative"
                    };
                }
            }
        }

        if (dto.WarehouseId.HasValue)
        {
            var warehouse = await _warehouseDataReader.GetByIdAsync(dto.WarehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
            {
                return new UpdatePurchaseOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.WarehouseIsNotFound"
                };
            }
        }

        var updatePurchaseOrderDto = new UpdatePurchaseOrderDto(dto.Id)
        {
            PlacedOnUtc = canChangeDate ? dto.PlacedOnUtc : purchaseOrder.PlacedOnUtc,
            VendorId = canChangeVendor ? dto.VendorId : purchaseOrder.VendorId,
            WarehouseId = dto.WarehouseId,
            ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc,
            Note = dto.Note,
            ShippingAmount = canChangeFees ? dto.ShippingAmount : purchaseOrder.ShippingAmount,
            TaxAmount = canChangeFees ? dto.TaxAmount : purchaseOrder.TaxAmount
        };

        var result = await _purchaseOrderManager.UpdatePurchaseOrderAsync(updatePurchaseOrderDto).ConfigureAwait(false);

        return new UpdatePurchaseOrderResultAppDto
        {
            Success = true,
            UpdatedId = result.Id
        };
    }

    public async Task<CommonActionResultDto> AddPurchaseOrderItemAsync(AddPurchaseOrderItemAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return CommonActionResultDto.CreateError(errorMessage);

        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!await _purchaseOrderManager.CanAddPurchaseOrderItemsAsync(dto.PurchaseOrderId).ConfigureAwait(false))
            return CommonActionResultDto.CreateError("Error.PurchaseOrderCannotAddItems");

        var product = await _productDataReader.GetByIdAsync(dto.ProductId).ConfigureAwait(false);
        if (product is null)
            return CommonActionResultDto.CreateError("Error.ProductIsNotFound");

        var result = await _purchaseOrderManager.AddPurchaseOrderItemAsync(new AddPurchaseOrderItemDto
        {
            ProductId = dto.ProductId,
            PurchaseOrderId = dto.PurchaseOrderId,
            QuantityOrdered = dto.QuantityOrdered,
            UnitCost = dto.UnitCost,
            Note = dto.Note
        });
        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> ChangeStatusAsync(Guid purchaseOrderId, int status)
    {
        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!await _purchaseOrderManager.CanChangeStatusToAsync(purchaseOrderId, (PurchaseOrderStatus)status))
            return CommonActionResultDto.CreateError("Error.OrderCannotChangeStatus");

        await _purchaseOrderManager.ChangeStatusAsync(purchaseOrderId, (PurchaseOrderStatus)status).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> ReceiveItemAsync(ReceivedGoodsForItemAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return CommonActionResultDto.CreateError(errorMessage);

        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!await _purchaseOrderManager.CanReceiveGoodsAsync(dto.PurchaseOrderId).ConfigureAwait(false))
            return CommonActionResultDto.CreateError("Error.PurchaseOrderCannotReceiveGoods");

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == dto.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderItemIsNotFound");

        var originalReceivedQuantity = dto.ReceivedQuantity;
        var maxReceivable = purchaseOrderItem.QuantityOrdered - purchaseOrderItem.QuantityReceived;

        if (originalReceivedQuantity > maxReceivable)
        {
            if (dto.OversupplyAction == "RejectOversupply")
            {
                dto = dto with { ReceivedQuantity = maxReceivable };
            }
            else if (dto.OversupplyAction == "AcceptToMainWarehouse")
            {
                dto = dto with { ReceivedQuantity = maxReceivable };
            }
            else
            {
                return CommonActionResultDto.CreateError("Error.PurchaseOrderReceiveQuantityExceedsOrdered");
            }
        }

        var product = await _productDataReader.GetByIdAsync(purchaseOrderItem.ProductId).ConfigureAwait(false);
        if (product is null)
            return CommonActionResultDto.CreateError("Error.ProductIsNotFound");


        if (dto.ReceivedByUserId.HasValue)
        {
            var user = await _userDataReader.GetByIdAsync(dto.ReceivedByUserId.Value).ConfigureAwait(false);
            if (user is null)
                return CommonActionResultDto.CreateError("Error.UserIsNotFound");
        }

        Guid? warehouseId = purchaseOrder.WarehouseId ?? dto.WarehouseId ?? null;
        if (!warehouseId.HasValue)
            return CommonActionResultDto.CreateError("Error.WarehouseRequired");

        else
        {
            var warehouse = await _warehouseDataReader.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
                return CommonActionResultDto.CreateError("Error.WarehouseIsNotFound");
        }

        var result = await _purchaseOrderManager.ReceiveItemsAsync(new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            ReceivedByUserId = dto.ReceivedByUserId,
            ReceivedQuantity = dto.ReceivedQuantity,
            WarehouseId = warehouseId,
            SellingPrice = dto.SellingPrice
        });

        if (originalReceivedQuantity > maxReceivable && dto.OversupplyAction == "AcceptToMainWarehouse")
        {
            var oversupplyQty = originalReceivedQuantity - maxReceivable;
            await _purchaseOrderManager.AcceptOversupplyToMainWarehouseAsync(
                dto.PurchaseOrderId, dto.PurchaseOrderItemId, oversupplyQty, warehouseId!.Value)
                .ConfigureAwait(false);
        }

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<BulkReceiveGoodsResultAppDto> BulkReceiveAsync(BulkReceiveGoodsAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return BulkReceiveGoodsResultAppDto.CreateError(errorMessage);

        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return BulkReceiveGoodsResultAppDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!await _purchaseOrderManager.CanReceiveGoodsAsync(dto.PurchaseOrderId).ConfigureAwait(false))
            return BulkReceiveGoodsResultAppDto.CreateError("Error.PurchaseOrderCannotReceiveGoods");

        if (dto.ReceivedByUserId.HasValue)
        {
            var user = await _userDataReader.GetByIdAsync(dto.ReceivedByUserId.Value).ConfigureAwait(false);
            if (user is null)
                return BulkReceiveGoodsResultAppDto.CreateError("Error.UserIsNotFound");
        }

        // Pre-validate ở app service để trả error messages thân thiện thay vì để manager throw exception.
        // Manager vẫn re-validate (defense-in-depth).

        // Bước 1: aggregate-validate qty theo PO item.
        var groupedByItem = dto.Items.GroupBy(i => i.ItemId);
        foreach (var group in groupedByItem)
        {
            var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == group.Key);
            if (purchaseOrderItem is null)
                return BulkReceiveGoodsResultAppDto.CreateError("Error.PurchaseOrderItemIsNotFound");

            var totalReceiving = group.Sum(x => x.Quantity);
            if (purchaseOrderItem.QuantityReceived + totalReceiving > purchaseOrderItem.QuantityOrdered)
                return BulkReceiveGoodsResultAppDto.CreateError("Error.PurchaseOrderReceiveQuantityExceedsOrdered");
        }

        // Bước 2: resolve + validate warehouse cho từng line (fallback về PO default nếu line không khai).
        var warehouseCache = new Dictionary<Guid, Warehouse?>();
        var lines = new List<BulkReceiveGoodsForPurchaseOrderLineDto>(dto.Items.Count);
        foreach (var itemDto in dto.Items)
        {
            var warehouseId = itemDto.WarehouseId ?? purchaseOrder.WarehouseId;
            if (!warehouseId.HasValue)
                return BulkReceiveGoodsResultAppDto.CreateError("Error.WarehouseRequired");

            if (!warehouseCache.TryGetValue(warehouseId.Value, out var warehouse))
            {
                warehouse = await _warehouseDataReader.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
                warehouseCache[warehouseId.Value] = warehouse;
            }
            if (warehouse is null)
                return BulkReceiveGoodsResultAppDto.CreateError("Error.WarehouseIsNotFound");

            lines.Add(new BulkReceiveGoodsForPurchaseOrderLineDto
            {
                PurchaseOrderItemId = itemDto.ItemId,
                WarehouseId = warehouseId.Value,
                ReceivedQuantity = itemDto.Quantity,
                ActualUnitCost = itemDto.ActualUnitCost
            });
        }

        // Bước 3: manager group-by-warehouse → mỗi kho = 1 GoodsReceipt, 1 lần UpdateAsync PO.
        var bulkResult = await _purchaseOrderManager.BulkReceiveItemsAsync(new BulkReceiveGoodsForPurchaseOrderDto(dto.PurchaseOrderId)
        {
            Lines = lines,
            ReceivedByUserId = dto.ReceivedByUserId
        }).ConfigureAwait(false);

        // Cộng dồn phí vận chuyển và thuế vào đơn (nếu có)
        if (dto.AdditionalShipping > 0 || dto.AdditionalTax > 0)
        {
            await _purchaseOrderManager.AddReceiptFeesAsync(dto.PurchaseOrderId, dto.AdditionalShipping, dto.AdditionalTax)
                .ConfigureAwait(false);
        }

        return BulkReceiveGoodsResultAppDto.CreateSuccess(bulkResult.CreatedGoodsReceiptIds);
    }

    public async Task<string> NextPurchaseOrderCodeAsync()
    {
        var code = string.Empty;
        var now = DateTime.UtcNow;
        var monthDateStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthDateEnd = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59, DateTimeKind.Utc);
        var monthOrderCount = await Task.Run(() => _purchaseOrderDataReader.DataSource.Where(o => o.CreatedOnUtc >= monthDateStart && o.CreatedOnUtc <= monthDateEnd).Count()).ConfigureAwait(false);
        do
        {
            code = $"{PurchaseOrder.PurchaseOrderCodePrefix}{now:MMyy}{++monthOrderCount:D3}";
        }
        while (await _purchaseOrderManager.DoesCodeExistAsync(code).ConfigureAwait(false));

        return code;
    }

    public async Task<CommonActionResultDto> SubmitsPurchaseOrderAsync(Guid id)
    {
        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(id).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!await _purchaseOrderManager.CanChangeStatusToAsync(id, PurchaseOrderStatus.Submitted))
            return CommonActionResultDto.CreateError("Error.PurchaseOrderCannotSubmit");

        await _purchaseOrderManager.ChangeStatusAsync(id, PurchaseOrderStatus.Submitted).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> ApprovePurchaseOrderAsync(Guid id)
    {
        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(id).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!await _purchaseOrderManager.CanChangeStatusToAsync(id, PurchaseOrderStatus.Approved))
            return CommonActionResultDto.CreateError("Error.Error.PurchaseOrderCannotChangeStatus");

        await _purchaseOrderManager.ChangeStatusAsync(id, PurchaseOrderStatus.Approved).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> CancelPurchaseOrderAsync(Guid id)
    {
        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(id).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!await _purchaseOrderManager.CanChangeStatusToAsync(id, PurchaseOrderStatus.Cancelled))
            return CommonActionResultDto.CreateError("Error.PurchaseOrderCannotCancel");

        await _purchaseOrderManager.ChangeStatusAsync(id, PurchaseOrderStatus.Cancelled).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> ClosePartialPurchaseOrderAsync(Guid id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return CommonActionResultDto.CreateError("Error.PurchaseOrder.CloseReasonRequired");

        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(id).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (purchaseOrder.Status != PurchaseOrderStatus.Receiving)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderCannotClosePartial");

        await _purchaseOrderManager.ClosePartialAsync(id, reason).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> DeletePurchaseOrderItemAsync(DeletePurchaseOrderItemAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == dto.ItemId);
        if (purchaseOrderItem is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderItemIsNotFound");

        // Can only delete items from Draft status
        if (purchaseOrder.Status != PurchaseOrderStatus.Draft)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderCannotDeleteItems");

        await _purchaseOrderManager.DeleteOrderItemAsync(dto.PurchaseOrderId, dto.ItemId).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<IList<RecentPurchasePriceAppDto>> GetRecentPurchasePricesAsync(Guid productId)
    {
        var domainDtos = await _purchaseOrderManager.GetRecentPurchasePricesAsync(productId).ConfigureAwait(false);

        return domainDtos
            .Select(d => new RecentPurchasePriceAppDto(
                VendorId: d.VendorId,
                VendorName: d.VendorName,
                UnitCost: d.UnitCost,
                PurchaseOrderCode: d.PurchaseOrderCode,
                PurchaseDate: d.PurchaseDateUtc.ToLocalTime()))
            .ToList();
    }

    public async Task<IList<OrderAllocatedPurchaseOrderAppDto>> GetAllocatedPurchaseOrdersForOrderAsync(Guid orderId)
    {
        var domainDtos = await _purchaseOrderAllocationManager.GetAllocatedPurchaseOrdersForOrderAsync(orderId).ConfigureAwait(false);
        return domainDtos
            .Select(dto => new OrderAllocatedPurchaseOrderAppDto
            {
                PurchaseOrderId = dto.PurchaseOrderId,
                PurchaseOrderCode = dto.PurchaseOrderCode,
                Status = (int)dto.Status,
                VendorId = dto.VendorId,
                VendorName = dto.VendorName,
                CreatedOnUtc = dto.CreatedOnUtc,
                ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc,
                Items = dto.Items.Select(item => new OrderAllocatedPurchaseOrderItemAppDto
                {
                    OrderItemId = item.OrderItemId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    AllocatedQuantity = item.AllocatedQuantity,
                    ReceivedQuantity = item.ReceivedQuantity
                }).ToList()
            })
            .ToList();
    }

    public async Task<CommonActionResultDto> UpdateAllocationDirectShipInfoAsync(
        Guid allocationId, string address, string? contactName, string? contactPhone, int priority)
    {
        if (_directShipManager == null)
            return CommonActionResultDto.CreateError("Error.DirectShipNotConfigured");
        try
        {
            await _directShipManager.MarkAllocationAsDirectShipAsync(
                allocationId, address, contactName, contactPhone, priority)
                .ConfigureAwait(false);
            return CommonActionResultDto.CreateSuccess();
        }
        catch (Exception ex)
        {
            return CommonActionResultDto.CreateError(ex.Message);
        }
    }
}

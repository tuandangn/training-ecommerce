using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.PurchaseOrders;

public sealed class PurchaseOrderAppService : IPurchaseOrderAppService
{
    private readonly IPurchaseOrderManager _purchaseOrderManager;
    private readonly IEntityDataReader<Vendor> _vendorDataReader;
    private readonly IEntityDataReader<Warehouse> _warehouseDataReader;
    private readonly IEntityDataReader<User> _userDataReader;
    private readonly IEntityDataReader<Product> _productDataReader;

    public PurchaseOrderAppService(IPurchaseOrderManager purchaseOrderManager,
        IEntityDataReader<Vendor> vendorDataReader, IEntityDataReader<Warehouse> warehouseDataReader,
        IEntityDataReader<User> userDataReader, IEntityDataReader<Product> productDataReader)
    {
        _purchaseOrderManager = purchaseOrderManager;
        _vendorDataReader = vendorDataReader;
        _warehouseDataReader = warehouseDataReader;
        _userDataReader = userDataReader;
        _productDataReader = productDataReader;
    }

    public async Task<IPagedDataAppDto<PurchaseOrderAppDto>> GetPurchaseOrdersAsync(string? keywords, int pageIndex, int pageSize)
    {
        var pagedData = await _purchaseOrderManager.GetPurchaseOrdersAsync(keywords, pageIndex, pageSize).ConfigureAwait(false);

        return PagedDataAppDto.Create(pagedData.Items.Select(item => item.ToDto()), pageIndex, pageSize, pagedData.PagerInfo.TotalCount);
    }

    public async Task<PurchaseOrderAppDto?> GetPurchaseOrderByIdAsync(Guid id)
    {
        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(id).ConfigureAwait(false);
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

        if (dto.CreatedByUserId.HasValue)
        {
            var user = await _userDataReader.GetByIdAsync(dto.CreatedByUserId.Value).ConfigureAwait(false);
            if (user is null)
            {
                return new CreatePurchaseOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = $"User with ID {dto.CreatedByUserId} does not exist."
                };
            }
        }

        if (dto.VendorId.HasValue)
        {
            var vendor = await _vendorDataReader.GetByIdAsync(dto.VendorId.Value).ConfigureAwait(false);
            if (vendor is null)
            {
                return new CreatePurchaseOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = $"Vendor with ID {dto.VendorId.Value} does not exist."
                };
            }
        }

        if (dto.WarehouseId.HasValue)
        {
            var warehouse = await _warehouseDataReader.GetByIdAsync(dto.WarehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
            {
                return new CreatePurchaseOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = $"Warehouse with ID {dto.WarehouseId.Value} does not exist."
                };
            }
        }

        var code = await NextPurchaseOrderCodeAsync().ConfigureAwait(false);
        var createPurchaseOrderDto = new CreatePurchaseOrderDto
        {
            Code = code,
            CreatedByUserId = dto.CreatedByUserId,
            VendorId = dto.VendorId,
            WarehouseId = dto.WarehouseId,
            ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc,
            Note = dto.Note,
            ShippingAmount = dto.ShippingAmount,
            TaxAmount = dto.TaxAmount
        };

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
                ErrorMessage = $"Purchase order with ID {dto.Id} does not exist."
            };
        }

        if (purchaseOrder.Status == PurchaseOrderStatus.Submitted
            || purchaseOrder.Status == PurchaseOrderStatus.Cancelled
            || purchaseOrder.Status == PurchaseOrderStatus.Completed)
        {
            return new UpdatePurchaseOrderResultAppDto
            {
                Success = false,
                ErrorMessage = $"Purchase order in status cannot change info"
            };
        }

        if (purchaseOrder.Status != PurchaseOrderStatus.Draft)
        {
            if (dto.VendorId != purchaseOrder.VendorId)
            {
                return new UpdatePurchaseOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = $"Purchase order in status cannot change vendor"
                };
            }
            if (dto.ExpectedDeliveryDateUtc?.Date != purchaseOrder.ExpectedDeliveryDateUtc?.Date)
            {
                return new UpdatePurchaseOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = $"Purchase order in status cannot change date"
                };
            }
        }

        if (purchaseOrder.Items.Count == 0)
        {
            if (dto.ShippingAmount > 0)
            {
                return new UpdatePurchaseOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = $"Purchase order cannot set shipping amount"
                };
            }
            if (dto.TaxAmount > 0)
            {
                return new UpdatePurchaseOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = $"Purchase order cannot set tax amount"
                };
            }
        }

        if (dto.VendorId.HasValue)
        {
            var vendor = await _vendorDataReader.GetByIdAsync(dto.VendorId.Value).ConfigureAwait(false);
            if (vendor is null)
            {
                return new UpdatePurchaseOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = $"Vendor with ID {dto.VendorId.Value} does not exist."
                };
            }
        }

        var updatePurchaseOrderDto = new UpdatePurchaseOrderDto(dto.Id)
        {
            VendorId = dto.VendorId,
            ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc,
            Note = dto.Note,
            ShippingAmount = dto.ShippingAmount,
            TaxAmount = dto.TaxAmount
        };

        var result = await _purchaseOrderManager.UpdatePurchaseOrderAsync(updatePurchaseOrderDto).ConfigureAwait(false);

        return new UpdatePurchaseOrderResultAppDto
        {
            Success = true,
            UpdatedId = result.Id
        };
    }

    public async Task<AddPurchaseOrderItemResultAppDto> AddPurchaseOrderItemAsync(AddPurchaseOrderItemAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new AddPurchaseOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
        {
            return new AddPurchaseOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = $"Purchase order with ID {dto.PurchaseOrderId} does not exist."
            };
        }
        if (!await _purchaseOrderManager.CanAddPurchaseOrderItemsAsync(dto.PurchaseOrderId).ConfigureAwait(false))
        {
            return new AddPurchaseOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = $"Cannot add item to purchase order with status {purchaseOrder.Status}."
            };
        }

        var product = await _productDataReader.GetByIdAsync(dto.ProductId).ConfigureAwait(false);
        if (product is null)
            return new AddPurchaseOrderItemResultAppDto
            {
                Success = false,
                ErrorMessage = $"Product with ID {dto.ProductId} does not exist."
            };

        var result = await _purchaseOrderManager.AddPurchaseOrderItemAsync(new AddPurchaseOrderItemDto
        {
            ProductId = dto.ProductId,
            PurchaseOrderId = dto.PurchaseOrderId,
            QuantityOrdered = dto.QuantityOrdered,
            UnitCost = dto.UnitCost,
            Note = dto.Note
        });
        return new AddPurchaseOrderItemResultAppDto
        {
            Success = true,
            PurchaseOrderId = result.PurchaseOrderId,
            CreatedItemId = result.CreatedItemId
        };
    }

    public async Task<(bool success, string? errorMessage)> ChangeStatusAsync(Guid purchaseOrderId, int status)
    {
        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return (false, $"Purchase order with ID {purchaseOrderId} does not exist.");

        if (!await _purchaseOrderManager.CanChangeStatusToAsync(purchaseOrderId, (PurchaseOrderStatus)status))
            return (false, $"Cannot change purchase order status from {purchaseOrder.Status} to {(PurchaseOrderStatus)status}.");

        await _purchaseOrderManager.ChangeStatusAsync(purchaseOrderId, (PurchaseOrderStatus)status).ConfigureAwait(false);

        return (true, null);
    }

    public async Task<ReceivedGoodsForItemResultAppDto> ReceiveItemAsync(ReceivedGoodsForItemAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new ReceivedGoodsForItemResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
        {
            return new ReceivedGoodsForItemResultAppDto
            {
                Success = false,
                ErrorMessage = $"Purchase order with ID {dto.PurchaseOrderId} does not exist."
            };
        }
        if (!await _purchaseOrderManager.CanReceiveGoodsAsync(dto.PurchaseOrderId).ConfigureAwait(false))
        {
            return new ReceivedGoodsForItemResultAppDto
            {
                Success = false,
                ErrorMessage = $"Cannot receive goods to purchase order {dto.PurchaseOrderId}."
            };
        }

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == dto.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
        {
            return new ReceivedGoodsForItemResultAppDto
            {
                Success = false,
                ErrorMessage = $"Purchase order item with ID {dto.PurchaseOrderItemId} does not exist in purchase order {dto.PurchaseOrderId}."
            };
        }

        if (purchaseOrderItem.QuantityReceived + dto.ReceivedQuantity > purchaseOrderItem.QuantityOrdered)
        {
            return new ReceivedGoodsForItemResultAppDto
            {
                Success = false,
                ErrorMessage = $"Cannot receive quantity {dto.ReceivedQuantity} for purchase order item {dto.PurchaseOrderItemId} because it exceeds the quantity ordered."
            };
        }

        var product = await _productDataReader.GetByIdAsync(purchaseOrderItem.ProductId).ConfigureAwait(false);
        if (product is null)
        {
            return new ReceivedGoodsForItemResultAppDto
            {
                Success = false,
                ErrorMessage = $"Product with ID {purchaseOrderItem.ProductId} does not exist."
            };
        }

        if (dto.ReceivedByUserId.HasValue)
        {
            var user = await _userDataReader.GetByIdAsync(dto.ReceivedByUserId.Value).ConfigureAwait(false);
            if (user is null)
            {
                return new ReceivedGoodsForItemResultAppDto
                {
                    Success = false,
                    ErrorMessage = $"User with ID {dto.ReceivedByUserId} does not exist."
                };
            }
        }

        Guid? warehouseId = purchaseOrder.WarehouseId ?? dto.WarehouseId ?? null;
        if (!warehouseId.HasValue)
        {
            return new ReceivedGoodsForItemResultAppDto
            {
                Success = false,
                ErrorMessage = "Warehouse is required."
            };
        }
        else
        {
            var warehouse = await _warehouseDataReader.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
            {
                return new ReceivedGoodsForItemResultAppDto
                {
                    Success = false,
                    ErrorMessage = $"Warehouse with ID {dto.WarehouseId} does not exist."
                };
            }
        }

        var result = await _purchaseOrderManager.ReceiveItemsAsync(new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            ReceivedByUserId = dto.ReceivedByUserId,
            ReceivedQuantity = dto.ReceivedQuantity,
            WarehouseId = warehouseId
        });
        return new ReceivedGoodsForItemResultAppDto
        {
            Success = true,
            ReceivedQuantity = result.ReceivedQuantity
        };
    }

    public async Task<string> NextPurchaseOrderCodeAsync()
    {
        var now = DateTime.UtcNow;
        var code = string.Empty;
        do
        {
            code = $"PO-{now:yyyyMM}-{Random.Shared.Next(1000, 9999)}";
        }
        while (await _purchaseOrderManager.DoesCodeExistAsync(code).ConfigureAwait(false));

        return code;
    }

    public async Task<(bool success, string? errorMessage)> SubmitsPurchaseOrderAsync(Guid id)
    {
        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(id).ConfigureAwait(false);
        if (purchaseOrder is null)
            return (false, $"Purchase order with ID {id} does not exist.");

        if (!await _purchaseOrderManager.CanChangeStatusToAsync(id, PurchaseOrderStatus.Submitted))
            return (false, "Cannot submit this purchase order");

        await _purchaseOrderManager.ChangeStatusAsync(id, PurchaseOrderStatus.Submitted).ConfigureAwait(false);

        return (true, null);
    }

    public async Task<(bool success, string? errorMessage)> CancelPurchaseOrderAsync(Guid id)
    {
        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(id).ConfigureAwait(false);
        if (purchaseOrder is null)
            return (false, $"Purchase order with ID {id} does not exist.");

        if (!await _purchaseOrderManager.CanChangeStatusToAsync(id, PurchaseOrderStatus.Cancelled))
            return (false, "Cannot cancel this purchase order");

        await _purchaseOrderManager.ChangeStatusAsync(id, PurchaseOrderStatus.Cancelled).ConfigureAwait(false);

        return (true, null);
    }
}

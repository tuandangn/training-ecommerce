using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.PurchaseOrders;

public sealed class PurchaseOrderAppService(IPurchaseOrderManager purchaseOrderManager,
    IPurchaseOrderAllocationManager purchaseOrderAllocationManager,
    IEntityDataReader<PurchaseOrderItemAllocation> purchaseOrderItemAllocationDataReader,
    IEntityDataReader<Vendor> vendorDataReader, IEntityDataReader<Warehouse> warehouseDataReader, IEntityDataReader<User> userDataReader,
    IEntityDataReader<Product> productDataReader, IDirectShipManager directShipManager,
    IEntityDataReader<DeliveryNote> deliveryNoteDataReader, IEntityDataReader<Order> orderDataReader,
    IEntityDataReader<Customer> customerDataReader, IEntityDataReader<UnitMeasurement> unitMeasurementDataReader,
    ICurrentUserAccessor currentUserAccessor, IRepository<PurchaseOrder> purchaseOrderRepository,
    IVendorDebtAppService vendorDebtAppService, IBankAccountAppService bankAccountAppService) : IPurchaseOrderAppService
{
    public async Task<IPagedDataAppDto<PurchaseOrderAppDto>> GetPurchaseOrdersAsync(int pageIndex, int pageSize, string? keywords, int? status)
    {
        PurchaseOrderStatus? poStatus = status.HasValue ? (PurchaseOrderStatus)status.Value : null;
        var pagedData = await purchaseOrderManager.GetPurchaseOrdersAsync(pageIndex, pageSize, keywords, poStatus).ConfigureAwait(false);

        return PagedDataAppDto.Create(pagedData.Items.Select(item => item.ToDto()), pageIndex, pageSize, pagedData.PagerInfo.TotalCount);
    }

    public async Task<PurchaseOrderAppDto?> GetPurchaseOrderByIdAsync(Guid id)
    {
        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(id).ConfigureAwait(false);
        if (purchaseOrder is null)
            return null;

        return purchaseOrder.ToDto();
    }

    public async Task<PurchaseOrderAppDto?> GetPurchaseOrderByCodeAsync(string code)
    {
        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByCodeAsync(code).ConfigureAwait(false);
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

        var vendor = await vendorDataReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor is null)
        {
            return new CreatePurchaseOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.VendorIsNotFound"
            };
        }

        var products = await productDataReader.GetByIdsAsync(dto.Items.Select(item => item.ProductId).OfType<Guid>()).ConfigureAwait(false);
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
            var warehouse = await warehouseDataReader.GetByIdAsync(dto.WarehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
            {
                return new CreatePurchaseOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.WarehouseIsNotFound"
                };
            }
        }

        foreach (var item in dto.Items)
        {
            var product = products.FirstOrDefault(product => product.Id == item.ProductId);
            if (product is null)
            {
                return new CreatePurchaseOrderResultAppDto
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
                        return new CreatePurchaseOrderResultAppDto
                        {
                            Success = false,
                            ErrorMessage = "Error.QuantityMustBeInteger"
                        };
                    }
                }
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
            ShippingAmount = dto.ShippingAmount,
            Items = dto.Items.Select(item => new CreatePurchaseOrderDto.CreatedPurchaseOrderItemDto
            {
                ProductId = item.ProductId ?? Guid.Empty,
                QuantityOrdered = item.Quantity,
                UnitCost = item.UnitCost,
                Note = item.Note
            }).ToList()
        };
        var result = await purchaseOrderManager.CreatePurchaseOrderAsync(createPurchaseOrderDto).ConfigureAwait(false);

        return new CreatePurchaseOrderResultAppDto
        {
            Success = true,
            CreatedId = result.CreatedId
        };
    }

    public async Task<PurchaseOrderQuickCreateResultAppDto> QuickCreatePurchaseOrderAsync(PurchaseOrderQuickCreateAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new PurchaseOrderQuickCreateResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var vendor = await vendorDataReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor is null)
        {
            return new PurchaseOrderQuickCreateResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.VendorIsNotFound"
            };
        }

        var productIds = dto.Items.Select(item => item.ProductId).OfType<Guid>().Distinct().ToList();
        var products = await productDataReader.GetByIdsAsync(productIds).ConfigureAwait(false);
        if (products.Count() != productIds.Count)
        {
            return new PurchaseOrderQuickCreateResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.ProductIsNotFound"
            };
        }

        var candidateVendorIds = products.SelectMany(p => p.ProductVendors).Select(v => v.VendorId).Distinct().ToList();
        var validVendorIds = candidateVendorIds.Where(vendorId => products.All(p => p.ProductVendors.Any(v => v.VendorId == vendorId))).ToList();
        if (validVendorIds.Count == 0)
        {
            return new PurchaseOrderQuickCreateResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.PurchaseOrder.NoVendorsAppropriate"
            };
        }

        if (!validVendorIds.Contains(dto.VendorId))
        {
            return new PurchaseOrderQuickCreateResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.PurchaseOrder.VendorIsNotAppropriate"
            };
        }

        if (dto.IsReceived)
        {
            var defaultWarehouse = await warehouseDataReader.GetByIdAsync(dto.DefaultWarehouseId!.Value).ConfigureAwait(false);
            if (defaultWarehouse is null)
            {
                return new PurchaseOrderQuickCreateResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.WarehouseIsNotFound"
                };
            }
        }

        if (dto.IsPaid)
        {
            if (dto.Payment!.PaymentMethod == (int)PaymentMethod.BankTransfer)
            {
                if (!dto.Payment.BankAccountId.HasValue)
                {
                    return new PurchaseOrderQuickCreateResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Error.BankTransferMethodRequireBankAccount"
                    };
                }

                var bankAccount = await bankAccountAppService.GetBankAccountByIdAsync(dto.Payment.BankAccountId.Value).ConfigureAwait(false);
                if (bankAccount is null)
                {
                    return new PurchaseOrderQuickCreateResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Error.BankAccountIsNotFound"
                    };
                }
            }
        }

        foreach (var item in dto.Items)
        {
            if (item.WarehouseId.HasValue)
            {
                var warehouse = await warehouseDataReader.GetByIdAsync(item.WarehouseId.Value).ConfigureAwait(false);
                if (warehouse is null)
                {
                    return new PurchaseOrderQuickCreateResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Error.WarehouseIsNotFound"
                    };
                }
            }

            var product = products.FirstOrDefault(product => product.Id == item.ProductId);
            if (product is null)
            {
                return new PurchaseOrderQuickCreateResultAppDto
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
                        return new PurchaseOrderQuickCreateResultAppDto
                        {
                            Success = false,
                            ErrorMessage = "Error.QuantityMustBeInteger"
                        };
                    }
                }
            }
        }

        var createPurchaseOrderResult = await purchaseOrderManager.CreatePurchaseOrderAsync(new CreatePurchaseOrderDto
        {
            PlacedOnUtc = DateTime.UtcNow,
            VendorId = dto.VendorId,
            WarehouseId = dto.IsReceived ? dto.DefaultWarehouseId : null,
            Note = dto.Note,
            Items = dto.Items.Select(item => new CreatePurchaseOrderDto.CreatedPurchaseOrderItemDto
            {
                ProductId = item.ProductId,
                QuantityOrdered = item.Quantity,
                UnitCost = item.UnitCost ?? 0
            }).ToList(),
            ExpectedDeliveryDateUtc = dto.IsReceived || !dto.ExpectedDeliveryOnUtc.HasValue ? null : dto.ExpectedDeliveryOnUtc
        }).ConfigureAwait(false);

        await SubmitsPurchaseOrderAsync(createPurchaseOrderResult.CreatedId).ConfigureAwait(false);
        await ApprovePurchaseOrderAsync(createPurchaseOrderResult.CreatedId).ConfigureAwait(false);

        var purchaseOrder = await purchaseOrderRepository.GetByIdAsync(createPurchaseOrderResult.CreatedId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new InvalidOperationException("Purchase order is not found");

        Guid? currentUserId = null;
        if (dto.IsReceived || dto.IsPaid)
        {
            var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
            currentUserId = currentUser?.Id;
        }

        if (dto.IsReceived)
        {
            await purchaseOrderManager.BulkReceiveItemsAsync(new BulkReceiveGoodsDto(purchaseOrder.Id)
            {
                ReceivedByUserId = currentUserId,
                ReceivedOnUtc = dto.ReceivedOnUtc,
                Lines = purchaseOrder.Items.Select(item => new BulkReceiveGoodsLineDto
                {
                    PurchaseOrderItemId = item.Id,
                    ReceivedQuantity = item.QuantityOrdered,
                    WarehouseId = dto.DefaultWarehouseId!.Value,
                    ActualUnitCost = item.UnitCost
                }).ToList(),
                PictureIds = dto.PictureIds,
                TaxRate = dto.TaxRate,
                ShippingAmount = dto.ShippingAmount
            }).ConfigureAwait(false);
        }

        if (dto.IsPaid)
        {
            await vendorDebtAppService.RecordFlexiblePaymentForVendorAsync(new CreateVendorPaymentAppDto
            {
                VendorId = dto.VendorId,
                Amount = dto.Payment!.PaidAmount,
                PaidOnUtc = dto.ReceivedOnUtc ?? dto.PlacedOnUtc,
                PaymentMethod = dto.Payment.PaymentMethod,
                PurchaseOrderId = createPurchaseOrderResult.CreatedId,
                RecordedByUserId = currentUserId,
                PaymentType = (int)PaymentType.VendorDebtPayment,
                BankAccountId = dto.Payment.BankAccountId
            }).ConfigureAwait(false);
        }

        return new PurchaseOrderQuickCreateResultAppDto
        {
            Success = true,
            CreatedId = createPurchaseOrderResult.CreatedId
        };
    }

    public async Task<CreatePurchaseOrderResultAppDto> CopyPurchaseOrderAsync(Guid id)
    {
        var result = await purchaseOrderManager.CopyPurchaseOrderAsync(id).ConfigureAwait(false);
        return new CreatePurchaseOrderResultAppDto { Success = true, CreatedId = result.CreatedId };
    }

    public async Task<CreatePurchaseOrderResultAppDto> SplitPurchaseOrderAsync(SplitPurchaseOrderAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var validateResult = dto.Validate();
        if (!validateResult.valid)
            return new CreatePurchaseOrderResultAppDto { Success = false, ErrorMessage = validateResult.errorMessage };

        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return new CreatePurchaseOrderResultAppDto { Success = false, ErrorMessage = "Error.PurchaseOrderIsNotFound" };

        if (!purchaseOrder.CanAddItems)
            return new CreatePurchaseOrderResultAppDto { Success = false, ErrorMessage = "Error.PurchaseOrderCannotUpdateOrderItems" };

        var splitedItems = purchaseOrder.Items.Where(item => dto.Items.Any(i => i.ItemId == item.Id)).ToList();

        var products = await productDataReader.GetByIdsAsync(splitedItems.Select(item => item.ProductId).OfType<Guid>()).ConfigureAwait(false);
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

        if (!validVendorIds.Contains(purchaseOrder.VendorId))
        {
            return new CreatePurchaseOrderResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.PurchaseOrder.VendorIsNotAppropriate"
            };
        }

        if (purchaseOrder.WarehouseId.HasValue)
        {
            var warehouse = await warehouseDataReader.GetByIdAsync(purchaseOrder.WarehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
            {
                return new CreatePurchaseOrderResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.WarehouseIsNotFound"
                };
            }
        }

        foreach (var item in dto.Items)
        {
            var splitedItem = splitedItems.First(i => i.Id == item.ItemId);
            var product = products.FirstOrDefault(product => product.Id == splitedItem.ProductId);
            if (product is null)
            {
                return new CreatePurchaseOrderResultAppDto
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
                        return new CreatePurchaseOrderResultAppDto
                        {
                            Success = false,
                            ErrorMessage = "Error.QuantityMustBeInteger"
                        };
                    }
                }
            }
        }

        var result = await purchaseOrderManager.SplitPurchaseOrderAsync(new SplitPurchaseOrderDto
        {
            SourcePurchaseOrderId = dto.PurchaseOrderId,
            Items = dto.Items.Select(i => new SplitPurchaseOrderItemDto { ItemId = i.ItemId, Quantity = i.Quantity }).ToList()
        }).ConfigureAwait(false);

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

        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.Id).ConfigureAwait(false);
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
                var vendor = await vendorDataReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
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
            var warehouse = await warehouseDataReader.GetByIdAsync(dto.WarehouseId.Value).ConfigureAwait(false);
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

        var result = await purchaseOrderManager.UpdatePurchaseOrderAsync(updatePurchaseOrderDto).ConfigureAwait(false);

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

        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!purchaseOrder.CanAddItems)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderCannotAddItems");

        var product = await productDataReader.GetByIdAsync(dto.ProductId).ConfigureAwait(false);
        if (product is null)
            return CommonActionResultDto.CreateError("Error.ProductIsNotFound");

        if (product.UnitMeasurementId.HasValue)
        {
            var unitMeasurement = await unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value).ConfigureAwait(false);
            if (unitMeasurement is not null)
            {
                if (!NumberHelper.IsValidDecimalPlace(dto.QuantityOrdered, unitMeasurement.DecimalPlaces))
                    return CommonActionResultDto.CreateError("Error.QuantityMustBeInteger");
            }
        }

        var result = await purchaseOrderManager.AddPurchaseOrderItemAsync(new AddPurchaseOrderItemDto
        {
            ProductId = dto.ProductId,
            PurchaseOrderId = dto.PurchaseOrderId,
            QuantityOrdered = dto.QuantityOrdered,
            UnitCost = dto.UnitCost,
            Note = dto.Note
        });
        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> UpdatePurchaseOrderItemAsync(UpdatePurchaseOrderItemAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return CommonActionResultDto.CreateError(errorMessage);

        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == dto.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderItemIsNotFound");

        if (!purchaseOrder.CanAddItems)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderCannotUpdateOrderItems");

        var product = await productDataReader.GetByIdAsync(purchaseOrderItem.ProductId).ConfigureAwait(false);
        if (product is null)
            return CommonActionResultDto.CreateError("Error.ProductIsNotFound");

        if (product.UnitMeasurementId.HasValue)
        {
            var unitMeasurement = await unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value).ConfigureAwait(false);
            if (unitMeasurement is not null)
            {
                if (!NumberHelper.IsValidDecimalPlace(dto.Quantity, unitMeasurement.DecimalPlaces))
                    return CommonActionResultDto.CreateError("Error.QuantityMustBeInteger");
            }
        }

        await purchaseOrderManager.UpdatePurchaseOrderItemAsync(new UpdatePurchaseOrderItemDto
        {
            PurchaseOrderId = dto.PurchaseOrderId,
            PurchaseOrderItemId = dto.PurchaseOrderItemId,
            ProductId = purchaseOrderItem.ProductId,
            QuantityOrdered = dto.Quantity,
            UnitCost = dto.UnitCost,
            Note = dto.Note
        }).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> ChangeStatusAsync(Guid purchaseOrderId, int status)
    {
        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!await purchaseOrderManager.CanChangeStatusToAsync(purchaseOrderId, (PurchaseOrderStatus)status))
            return CommonActionResultDto.CreateError("Error.OrderCannotChangeStatus");

        await purchaseOrderManager.ChangeStatusAsync(purchaseOrderId, (PurchaseOrderStatus)status).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<ReceiveGoodsResultAppDto> ReceiveItemAsync(ReceiveGoodsAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return ReceiveGoodsResultAppDto.CreateError(errorMessage);

        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return ReceiveGoodsResultAppDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!await purchaseOrderManager.CanReceiveGoodsAsync(dto.PurchaseOrderId).ConfigureAwait(false))
            return ReceiveGoodsResultAppDto.CreateError("Error.PurchaseOrderCannotReceiveGoods");

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == dto.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
            return ReceiveGoodsResultAppDto.CreateError("Error.PurchaseOrderItemIsNotFound");

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
                return ReceiveGoodsResultAppDto.CreateError("Error.PurchaseOrderReceiveQuantityExceedsOrdered");
            }
        }

        var product = await productDataReader.GetByIdAsync(purchaseOrderItem.ProductId).ConfigureAwait(false);
        if (product is null)
            return ReceiveGoodsResultAppDto.CreateError("Error.ProductIsNotFound");

        if (dto.ReceivedByUserId.HasValue)
        {
            var user = await userDataReader.GetByIdAsync(dto.ReceivedByUserId.Value).ConfigureAwait(false);
            if (user is null)
                return ReceiveGoodsResultAppDto.CreateError("Error.UserIsNotFound");
        }

        var directShipAllocations = await directShipManager.GetDirectShipAllocationsForPoItemsAsync([(purchaseOrder.Id, purchaseOrderItem.Id)]).ConfigureAwait(false);
        var remainingAllocatedDirectShipQty = directShipAllocations.Sum(allocation => Math.Max(0, allocation.AllocatedQuantity - allocation.ReceivedQuantity));
        var physicalWarehouseRequired = dto.ReceivedQuantity > remainingAllocatedDirectShipQty;

        var warehouseId = dto.WarehouseId ?? purchaseOrder.WarehouseId;
        if (!warehouseId.HasValue && physicalWarehouseRequired)
            return ReceiveGoodsResultAppDto.CreateError("Error.WarehouseRequired");
        if (warehouseId.HasValue)
        {
            var warehouse = await warehouseDataReader.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
                return ReceiveGoodsResultAppDto.CreateError("Error.WarehouseIsNotFound");
            if (physicalWarehouseRequired && warehouse.WarehouseType != WarehouseType.Physical)
                return ReceiveGoodsResultAppDto.CreateError("Error.PhysicalWarehouseRequired");
        }

        Guid? createdGoodsReceiptId = null;
        var receiveDirectShipQty = Math.Min(remainingAllocatedDirectShipQty, dto.ReceivedQuantity);
        if (receiveDirectShipQty > 0)
        {
            var directTransitWarehouseId = await directShipManager.GetTransitWarehouseIdAsync().ConfigureAwait(false);
            var directShipReceiveResult = await purchaseOrderManager.ReceivesItemAsync(new ReceivedGoodsDto(purchaseOrder.Id, purchaseOrderItem.Id)
            {
                ReceivedByUserId = dto.ReceivedByUserId,
                ReceivedQuantity = receiveDirectShipQty,
                QuantityDecimalPlaces = dto.QuantityDecimalPlaces,
                TaxRate = dto.TaxRate,
                WarehouseId = directTransitWarehouseId,
                SellingPrice = null,
                ActualUnitCost = dto.ActualUnitCost
            });

            if (!physicalWarehouseRequired) createdGoodsReceiptId = directShipReceiveResult.CreatedGoodsReceiptId;
        }

        var receiveWarehouseQty = Math.Max(0, dto.ReceivedQuantity - receiveDirectShipQty);
        if (receiveWarehouseQty > 0)
        {
            var warehouseReceiveResult = await purchaseOrderManager.ReceivesItemAsync(new ReceivedGoodsDto(purchaseOrder.Id, purchaseOrderItem.Id)
            {
                ReceivedByUserId = dto.ReceivedByUserId,
                ReceivedQuantity = receiveWarehouseQty,
                QuantityDecimalPlaces = dto.QuantityDecimalPlaces,
                TaxRate = dto.TaxRate,
                WarehouseId = warehouseId,
                SellingPrice = dto.SellingPrice,
                ActualUnitCost = dto.ActualUnitCost
            });

            createdGoodsReceiptId = warehouseReceiveResult.CreatedGoodsReceiptId;
        }

        if (originalReceivedQuantity > maxReceivable && dto.OversupplyAction == "AcceptToMainWarehouse")
        {
            var oversupplyQty = originalReceivedQuantity - maxReceivable;
            if (!physicalWarehouseRequired)
                warehouseId = warehouseDataReader.DataSource.Where(warehouse => warehouse.WarehouseType == WarehouseType.Physical).FirstOrDefault()?.Id;
            if (!warehouseId.HasValue)
                throw new OversupplyQuantityCannotHandledException();
            await purchaseOrderManager.AcceptOversupplyToMainWarehouseAsync(
                dto.PurchaseOrderId, dto.PurchaseOrderItemId, oversupplyQty, warehouseId!.Value)
                .ConfigureAwait(false);
        }

        return ReceiveGoodsResultAppDto.CreateSuccess(dto.ReceivedQuantity, createdGoodsReceiptId);
    }

    public async Task<CommonActionResultDto> SetGoodsReceiptToPurchaseOrderAsync(SetGoodsReceiptToPurchaseOrderAppDto dto)
    {
        try
        {
            await purchaseOrderManager.SetGoodsReceiptToPurchaseOrderAsync(
                new SetGoodsReceiptToPurchaseOrderDto(dto.Id, dto.PurchaseOrderId)).ConfigureAwait(false);
            return CommonActionResultDto.CreateSuccess();
        }
        catch (NamEcommerceDomainException ex)
        {
            return CommonActionResultDto.CreateError(ex.ErrorCode);
        }
        catch (Exception ex)
        {
            return CommonActionResultDto.CreateError(ex.Message);
        }
    }

    public async Task<CommonActionResultDto> RemoveGoodsReceiptFromPurchaseOrderAsync(RemoveGoodsReceiptFromPurchaseOrderAppDto dto)
    {
        await purchaseOrderManager.RemoveGoodsReceiptFromPurchaseOrderAsync(
            new RemoveGoodsReceiptFromPurchaseOrderDto(dto.Id)).ConfigureAwait(false);
        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> SubmitsPurchaseOrderAsync(Guid id)
    {
        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(id).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!await purchaseOrderManager.CanChangeStatusToAsync(id, PurchaseOrderStatus.Submitted))
            return CommonActionResultDto.CreateError("Error.PurchaseOrderCannotSubmit");

        await purchaseOrderManager.ChangeStatusAsync(id, PurchaseOrderStatus.Submitted).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> ApprovePurchaseOrderAsync(Guid id)
    {
        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(id).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!await purchaseOrderManager.CanChangeStatusToAsync(id, PurchaseOrderStatus.Approved))
            return CommonActionResultDto.CreateError("Error.Error.PurchaseOrderCannotChangeStatus");

        await purchaseOrderManager.ChangeStatusAsync(id, PurchaseOrderStatus.Approved).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> CancelPurchaseOrderAsync(Guid id)
    {
        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(id).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!await purchaseOrderManager.CanChangeStatusToAsync(id, PurchaseOrderStatus.Cancelled))
            return CommonActionResultDto.CreateError("Error.PurchaseOrderCannotCancel");

        await purchaseOrderManager.CancelAsync(id).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> ClosePartialPurchaseOrderAsync(Guid id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return CommonActionResultDto.CreateError("Error.PurchaseOrder.CloseReasonRequired");

        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(id).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (purchaseOrder.Status != PurchaseOrderStatus.Receiving)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderCannotClosePartial");

        await purchaseOrderManager.ClosePartialAsync(id, reason).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> DeletePurchaseOrderItemAsync(DeletePurchaseOrderItemAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == dto.ItemId);
        if (purchaseOrderItem is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderItemIsNotFound");

        // Can only delete items from Draft status
        if (!purchaseOrder.CanAddItems)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderCannotDeleteItems");

        await purchaseOrderManager.DeleteOrderItemAsync(dto.PurchaseOrderId, dto.ItemId).ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<IList<RecentPurchasePriceAppDto>> GetRecentPurchasePricesAsync(Guid productId)
    {
        var domainDtos = await purchaseOrderManager.GetRecentPurchasePricesAsync(productId).ConfigureAwait(false);

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
        var domainDtos = await purchaseOrderAllocationManager.GetAllocatedPurchaseOrdersForOrderAsync(orderId).ConfigureAwait(false);
        return domainDtos
            .Select(dto => new OrderAllocatedPurchaseOrderAppDto
            {
                PurchaseOrderId = dto.PurchaseOrderId,
                PurchaseOrderCode = dto.PurchaseOrderCode,
                Status = (int)dto.Status,
                VendorId = dto.VendorId,
                VendorName = dto.VendorName,
                PlacedOnUtc = dto.PlacedOnUtc,
                ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc,
                Items = dto.Items.Select(item => new OrderAllocatedPurchaseOrderItemAppDto
                {
                    OrderId = item.OrderItemId.PrimaryId,
                    OrderItemId = item.OrderItemId.SecondaryId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    AllocatedQuantity = item.AllocatedQuantity,
                    ReceivedQuantity = item.ReceivedQuantity
                }).ToList()
            })
            .ToList();
    }

    public async Task<IList<EligibleOrderItemForAllocationAppDto>> GetEligibleOrderItemsForPoItemAsync((Guid purchaseOrderId, Guid purchaseOrderItemId) purchaseOrderItemId)
    {
        var domainDtos = await purchaseOrderAllocationManager.GetEligibleOrderItemsForPoItemAsync(purchaseOrderItemId).ConfigureAwait(false);
        return domainDtos.Select(d => new EligibleOrderItemForAllocationAppDto
        {
            OrderItemId = d.OrderItemId,
            OrderId = d.OrderId,
            OrderCode = d.OrderCode,
            CustomerName = d.CustomerName,
            ProductId = d.ProductId,
            ProductName = d.ProductName,
            TotalQuantity = d.TotalQuantity,
            AllocatedOutstanding = d.AllocatedOutstanding,
            AvailableToAllocate = d.AvailableToAllocate,
            ShippingAddress = d.ShippingAddress,
            CustomerPhone = d.CustomerPhone
        }).ToList();
    }

    public async Task<IList<NonDirectShipAllocationForPoItemAppDto>> GetNonDirectShipAllocationsForPoItemAsync((Guid primaryItemId, Guid secondaryItemId) purchaseOrderItemId)
    {
        var domainDtos = await purchaseOrderAllocationManager.GetNonDirectShipAllocationsForPoItemAsync(purchaseOrderItemId).ConfigureAwait(false);
        return domainDtos.Select(d => new NonDirectShipAllocationForPoItemAppDto
        {
            AllocationId = d.AllocationId,
            OrderId = d.OrderId,
            OrderItemId = d.OrderItemId,
            OrderCode = d.OrderCode,
            CustomerName = d.CustomerName,
            CustomerPhone = d.CustomerPhone,
            ShippingAddress = d.ShippingAddress,
            AllocatedQuantity = d.AllocatedQuantity,
            RemainingQuantity = d.RemainingQuantity
        }).ToList();
    }

    public Task<decimal> GetAllocationRemainingQuantityAsync(Guid allocationId)
    {
        var allocation = purchaseOrderItemAllocationDataReader.DataSource
            .FirstOrDefault(a => a.Id == allocationId);
        if (allocation is null)
            return Task.FromResult(0m);
        return Task.FromResult(Math.Max(0m, allocation.AllocatedQuantity - allocation.ReceivedQuantity));
    }

    public async Task<IList<PurchaseOrderItemAllocationForPoItemAppDto>> GetAllocationsForPurchaseOrderItemsAsync(IReadOnlyList<(Guid primaryItemId, Guid secondaryItemId)> purchaseOrderItemIds)
    {
        if (purchaseOrderItemIds.Count == 0)
            return [];

        var poItemIds = purchaseOrderItemIds.Select(id => id.secondaryItemId).Distinct().ToList();
        var allocations = purchaseOrderItemAllocationDataReader.DataSource
            .Where(allocation => poItemIds.Any(poItemId => poItemId == allocation.PurchaseOrderItemId.SecondaryId)
                && allocation.Status != AllocationStatus.Cancelled)
            .OrderBy(allocation => allocation.CreatedOnUtc)
            .ToList();
        if (allocations.Count == 0)
            return [];

        var orderItemIds = allocations.Select(allocation => allocation.OrderItemId.SecondaryId).Distinct().ToList();
        var orderItems = orderDataReader.DataSource
            .SelectMany(order => order.OrderItems
                .Where(item => orderItemIds.Contains(item.Id))
                .Select(item => new { Order = order, Item = item }))
            .ToDictionary(x => x.Item.Id);

        var customerIds = orderItems.Values.Select(x => x.Order.CustomerId).Distinct().ToList();
        var customers = customerDataReader.DataSource
            .Where(customer => customerIds.Contains(customer.Id))
            .ToDictionary(customer => customer.Id);

        var result = allocations
            .Where(allocation => orderItems.ContainsKey(allocation.OrderItemId.SecondaryId))
            .Select(allocation =>
            {
                var orderItem = orderItems[allocation.OrderItemId.SecondaryId];
                customers.TryGetValue(orderItem.Order.CustomerId, out var customer);
                return new PurchaseOrderItemAllocationForPoItemAppDto
                {
                    AllocationId = allocation.Id,
                    PurchaseOrderId = allocation.PurchaseOrderItemId.PrimaryId,
                    PurchaseOrderItemId = allocation.PurchaseOrderItemId.SecondaryId,
                    OrderId = orderItem.Order.Id,
                    OrderItemId = allocation.OrderItemId.SecondaryId,
                    OrderCode = orderItem.Order.Code,
                    CustomerName = customer?.FullName,
                    CustomerPhone = customer?.PhoneNumber,
                    ShippingAddress = orderItem.Order.ShippingAddress,
                    AllocatedQuantity = allocation.AllocatedQuantity,
                    ReceivedQuantity = allocation.ReceivedQuantity,
                    Status = (int)allocation.Status,
                    IsDirectShip = allocation.IsDirectShip
                };
            })
            .ToList();

        return result;
    }

    public async Task<CommonActionResultDto> AllocatePoItemForOrderItemAsync(AllocatePoItemForOrderItemAppDto dto)
    {
        var hasDirectShipInfo = !string.IsNullOrWhiteSpace(dto.DirectShipAddress)
            || !string.IsNullOrWhiteSpace(dto.DirectShipContactName)
            || !string.IsNullOrWhiteSpace(dto.DirectShipContactPhone);
        if (hasDirectShipInfo && string.IsNullOrWhiteSpace(dto.DirectShipContactPhone))
            return CommonActionResultDto.CreateError("Error.DirectShipContactPhoneRequired");
        if (hasDirectShipInfo && string.IsNullOrWhiteSpace(dto.DirectShipAddress))
            return CommonActionResultDto.CreateError("Error.DirectShipAddressRequired");

        var allocation = await purchaseOrderAllocationManager
            .AllocatePurchaseOrderItemForOrder(new AllocatePurchaseOrderItemForOrder
            {
                PurchaseOrderItemId = (dto.PurchaseOrderId, dto.PurchaseOrderItemId),
                OrderItemId = (dto.OrderId, dto.OrderItemId),
                AllocationQuantity = dto.Quantity,
                DirectShipInfo = hasDirectShipInfo
                    ? new AllocatePurchaseOrderItemForOrder.AllocateDirectShipInfo(dto.DirectShipContactName, dto.DirectShipContactPhone, dto.DirectShipAddress)
                    : null
            })
            .ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> ReleasePoItemAllocationForOrderItemAsync(ReleaseAllocationsOfPurchaseOrderItemAppDto dto)
    {
        await purchaseOrderAllocationManager
            .ReleaseAllocationsOfPurchaseOrderItemAsync((dto.PurchaseOrderId, dto.PurchaseOrderItemId))
            .ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<decimal> GetMaxAllocationQuantityForOrderItemAsync(Guid orderId, Guid orderItemId)
    {
        var order = await orderDataReader.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);

        var orderItem = order.OrderItems.FirstOrDefault(item => item.Id == orderItemId);
        if (orderItem is null)
            throw new OrderItemIsNotFoundException();

        var allocatedOutstanding = purchaseOrderItemAllocationDataReader.DataSource
            .Where(allocation => allocation.OrderItemId.SecondaryId == orderItemId && allocation.Status != AllocationStatus.Cancelled)
            .ToList()
            .Sum(allocation => Math.Max(0m, allocation.AllocatedQuantity - allocation.ReceivedQuantity));
        var activeDeliveryQuantity = deliveryNoteDataReader.DataSource
            .Where(deliveryNote => deliveryNote.Status != DeliveryNoteStatus.Cancelled)
            .SelectMany(deliveryNote => deliveryNote.Items)
            .Where(item => item.OrderItemId == orderItemId)
            .Sum(item => item.Quantity);

        return Math.Max(0m, orderItem.Quantity - activeDeliveryQuantity - allocatedOutstanding);
    }

    public async Task<BulkReceiveGoodsResultAppDto> BulkReceiveItemsAsync(BulkReceiveGoodsAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new BulkReceiveGoodsResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        foreach (var item in dto.Items)
        {
            if (!item.WarehouseId.HasValue)
            {
                return new BulkReceiveGoodsResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.WarehouseRequired"
                };
            }
            var warehouse = await warehouseDataReader.GetByIdAsync(item.WarehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
            {
                return new BulkReceiveGoodsResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.WarehouseIsNotFound"
                };
            }
        }

        var bulkReceiveGoodsResult = await purchaseOrderManager.BulkReceiveItemsAsync(new BulkReceiveGoodsDto(dto.PurchaseOrderId)
        {
            ReceivedByUserId = dto.ReceivedByUserId,
            ReceivedOnUtc = dto.ReceivedOnUtc,
            PictureIds = dto.PictureIds,
            ShippingAmount = dto.ShippingAmount,
            TaxRate = dto.TaxRate,
            Lines = dto.Items.Select(item => new BulkReceiveGoodsLineDto
            {
                PurchaseOrderItemId = item.PurchaseOrderItemId,
                WarehouseId = item.WarehouseId!.Value,
                ReceivedQuantity = item.ReceivedQuantity,
                ActualUnitCost = item.ActualUnitCost
            }).ToList()
        });

        return new BulkReceiveGoodsResultAppDto
        {
            Success = true,
            CreatedGoodsReceiptIds = bulkReceiveGoodsResult.CreatedGoodsReceiptIds
        };
    }
}

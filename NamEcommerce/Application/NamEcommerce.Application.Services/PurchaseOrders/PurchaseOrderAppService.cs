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
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.PurchaseOrders;

public sealed class PurchaseOrderAppService : IPurchaseOrderAppService
{
    private readonly IPurchaseOrderManager _purchaseOrderManager;
    private readonly IPurchaseOrderAllocationManager _purchaseOrderAllocationManager;
    private readonly IDirectShipManager _directShipManager;
    private readonly IEntityDataReader<Vendor> _vendorDataReader;
    private readonly IEntityDataReader<Warehouse> _warehouseDataReader;
    private readonly IEntityDataReader<User> _userDataReader;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IRepository<PurchaseOrder> _purchaseOrderRepository;
    private readonly IEntityDataReader<PurchaseOrderItemAllocation> _purchaseOrderItemAllocationDataReader;
    private readonly IEntityDataReader<DeliveryNote> _deliveryNoteDataReader;
    private readonly IEntityDataReader<Order> _orderDataReader;
    private readonly IEntityDataReader<Customer> _customerDataReader;
    private readonly IEntityDataReader<UnitMeasurement> _unitMeasurementDataReader;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IVendorDebtAppService _vendorDebtAppService;
    private readonly IBankAccountAppService _bankAccountAppService;

    public PurchaseOrderAppService(IPurchaseOrderManager purchaseOrderManager,
        IPurchaseOrderAllocationManager purchaseOrderAllocationManager,
        IEntityDataReader<PurchaseOrderItemAllocation> purchaseOrderItemAllocationDataReader,
        IEntityDataReader<Vendor> vendorDataReader, IEntityDataReader<Warehouse> warehouseDataReader, IEntityDataReader<User> userDataReader,
        IEntityDataReader<Product> productDataReader, IDirectShipManager directShipManager,
        IEntityDataReader<DeliveryNote> deliveryNoteDataReader, IEntityDataReader<Order> orderDataReader,
        IEntityDataReader<Customer> customerDataReader, IEntityDataReader<UnitMeasurement> unitMeasurementDataReader,
        ICurrentUserAccessor currentUserAccessor, IRepository<PurchaseOrder> purchaseOrderRepository,
        IVendorDebtAppService vendorDebtAppService, IBankAccountAppService bankAccountAppService)
    {
        _purchaseOrderManager = purchaseOrderManager;
        _purchaseOrderAllocationManager = purchaseOrderAllocationManager;
        _vendorDataReader = vendorDataReader;
        _warehouseDataReader = warehouseDataReader;
        _userDataReader = userDataReader;
        _productDataReader = productDataReader;
        _directShipManager = directShipManager;
        _purchaseOrderItemAllocationDataReader = purchaseOrderItemAllocationDataReader;
        _deliveryNoteDataReader = deliveryNoteDataReader;
        _orderDataReader = orderDataReader;
        _customerDataReader = customerDataReader;
        _unitMeasurementDataReader = unitMeasurementDataReader;
        _currentUserAccessor = currentUserAccessor;
        _purchaseOrderRepository = purchaseOrderRepository;
        _vendorDebtAppService = vendorDebtAppService;
        _bankAccountAppService = bankAccountAppService;
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
                var unitMeasurement = await _unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value).ConfigureAwait(false);
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
        var result = await _purchaseOrderManager.CreatePurchaseOrderAsync(createPurchaseOrderDto).ConfigureAwait(false);

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

        var vendor = await _vendorDataReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor is null)
        {
            return new PurchaseOrderQuickCreateResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.VendorIsNotFound"
            };
        }

        var productIds = dto.Items.Select(item => item.ProductId).OfType<Guid>().Distinct().ToList();
        var products = await _productDataReader.GetByIdsAsync(productIds).ConfigureAwait(false);
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
            var defaultWarehouse = await _warehouseDataReader.GetByIdAsync(dto.DefaultWarehouseId!.Value).ConfigureAwait(false);
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

                var bankAccount = await _bankAccountAppService.GetBankAccountByIdAsync(dto.Payment.BankAccountId.Value).ConfigureAwait(false);
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
                var warehouse = await _warehouseDataReader.GetByIdAsync(item.WarehouseId.Value).ConfigureAwait(false);
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
                var unitMeasurement = await _unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value).ConfigureAwait(false);
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

        var createPurchaseOrderResult = await _purchaseOrderManager.CreatePurchaseOrderAsync(new CreatePurchaseOrderDto
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
            ExpectedDeliveryDateUtc = dto.IsReceived || !dto.ExpectedDeliveryOnUtc.HasValue ? null : dto.ExpectedDeliveryOnUtc,
            ShippingAmount = dto.IsReceived && dto.ShippingAmount.HasValue ? dto.ShippingAmount.Value : 0,
            TaxAmount = dto.IsReceived && dto.TaxAmount.HasValue ? dto.TaxAmount.Value : 0
        }).ConfigureAwait(false);

        await SubmitsPurchaseOrderAsync(createPurchaseOrderResult.CreatedId).ConfigureAwait(false);
        await ApprovePurchaseOrderAsync(createPurchaseOrderResult.CreatedId).ConfigureAwait(false);

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(createPurchaseOrderResult.CreatedId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new InvalidOperationException("Purchase order is not found");

        Guid? currentUserId = null;
        if (dto.IsReceived || dto.IsPaid)
        {
            var currentUser = await _currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
            currentUserId = currentUser?.Id;
        }

        if (dto.IsReceived)
        {
            await _purchaseOrderManager.BulkReceiveItemsAsync(new BulkReceiveGoodsForPurchaseOrderDto(purchaseOrder.Id)
            {
                ReceivedByUserId = currentUserId,
                ReceivedOnUtc = dto.ReceivedOnUtc,
                Lines = purchaseOrder.Items.Select(item => new BulkReceiveGoodsForPurchaseOrderLineDto
                {
                    PurchaseOrderItemId = item.Id,
                    ReceivedQuantity = item.QuantityOrdered,
                    WarehouseId = dto.DefaultWarehouseId!.Value,
                    ActualUnitCost = item.UnitCost
                }).ToList(),
                PictureIds = dto.PictureIds
            }).ConfigureAwait(false);
        }

        if (dto.IsPaid)
        {
            await _vendorDebtAppService.RecordFlexiblePaymentForVendorAsync(new CreateVendorPaymentAppDto
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
        var result = await _purchaseOrderManager.CopyPurchaseOrderAsync(id).ConfigureAwait(false);
        return new CreatePurchaseOrderResultAppDto { Success = true, CreatedId = result.CreatedId };
    }

    public async Task<CreatePurchaseOrderResultAppDto> SplitPurchaseOrderAsync(SplitPurchaseOrderAppDto dto)
    {
        var result = await _purchaseOrderManager.SplitPurchaseOrderAsync(new SplitPurchaseOrderDto
        {
            SourcePurchaseOrderId = dto.PurchaseOrderId,
            Items = dto.Items.Select(i => new SplitPurchaseOrderItemDto { ItemId = i.ItemId, Quantity = i.Quantity }).ToList()
        }).ConfigureAwait(false);
        return new CreatePurchaseOrderResultAppDto { Success = true, CreatedId = result.CreatedId };
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

        if (product.UnitMeasurementId.HasValue)
        {
            var unitMeasurement = await _unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value).ConfigureAwait(false);
            if (unitMeasurement is not null)
            {
                if (!NumberHelper.IsValidDecimalPlace(dto.QuantityOrdered, unitMeasurement.DecimalPlaces))
                    return CommonActionResultDto.CreateError("Error.QuantityMustBeInteger");
            }
        }

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

    public async Task<CommonActionResultDto> UpdatePurchaseOrderItemAsync(UpdatePurchaseOrderItemAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return CommonActionResultDto.CreateError(errorMessage);

        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderIsNotFound");

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == dto.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderItemIsNotFound");

        if (!purchaseOrder.CanAddItems)
            return CommonActionResultDto.CreateError("Error.PurchaseOrderCannotUpdateOrderItems");

        await _purchaseOrderManager.UpdatePurchaseOrderItemAsync(new UpdatePurchaseOrderItemDto
        {
            PurchaseOrderId = dto.PurchaseOrderId,
            PurchaseOrderItemId = dto.PurchaseOrderItemId,
            ProductId = purchaseOrderItem.ProductId,
            QuantityOrdered = dto.QuantityOrdered,
            UnitCost = dto.UnitCost,
            Note = dto.Note
        }).ConfigureAwait(false);

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

    public async Task<ReceiveItemResultAppDto> ReceiveItemAsync(ReceivedGoodsForItemAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return ReceiveItemResultAppDto.CreateError(errorMessage);

        var purchaseOrder = await _purchaseOrderManager.GetPurchaseOrderByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return ReceiveItemResultAppDto.CreateError("Error.PurchaseOrderIsNotFound");

        if (!await _purchaseOrderManager.CanReceiveGoodsAsync(dto.PurchaseOrderId).ConfigureAwait(false))
            return ReceiveItemResultAppDto.CreateError("Error.PurchaseOrderCannotReceiveGoods");

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == dto.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
            return ReceiveItemResultAppDto.CreateError("Error.PurchaseOrderItemIsNotFound");

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
                return ReceiveItemResultAppDto.CreateError("Error.PurchaseOrderReceiveQuantityExceedsOrdered");
            }
        }

        var product = await _productDataReader.GetByIdAsync(purchaseOrderItem.ProductId).ConfigureAwait(false);
        if (product is null)
            return ReceiveItemResultAppDto.CreateError("Error.ProductIsNotFound");

        if (dto.ReceivedByUserId.HasValue)
        {
            var user = await _userDataReader.GetByIdAsync(dto.ReceivedByUserId.Value).ConfigureAwait(false);
            if (user is null)
                return ReceiveItemResultAppDto.CreateError("Error.UserIsNotFound");
        }

        var directShipAllocations = await _directShipManager.GetDirectShipAllocationsForPoItemsAsync([(purchaseOrder.Id, purchaseOrderItem.Id)]).ConfigureAwait(false);
        var remainingAllocatedDirectShipQty = directShipAllocations.Sum(allocation => Math.Max(0, allocation.AllocatedQuantity - allocation.ReceivedQuantity));
        var physicalWarehouseRequired = dto.ReceivedQuantity > remainingAllocatedDirectShipQty;

        var warehouseId = dto.WarehouseId ?? purchaseOrder.WarehouseId;
        if (!warehouseId.HasValue && physicalWarehouseRequired)
            return ReceiveItemResultAppDto.CreateError("Error.WarehouseRequired");
        if (warehouseId.HasValue)
        {
            var warehouse = await _warehouseDataReader.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
                return ReceiveItemResultAppDto.CreateError("Error.WarehouseIsNotFound");
            if (physicalWarehouseRequired && warehouse.WarehouseType != WarehouseType.Physical)
                return ReceiveItemResultAppDto.CreateError("Error.PhysicalWarehouseRequired");
        }

        Guid? createdGoodsReceiptId = null;
        var receiveDirectShipQty = Math.Min(remainingAllocatedDirectShipQty, dto.ReceivedQuantity);
        if (receiveDirectShipQty > 0)
        {
            var directTransitWarehouseId = await _directShipManager.GetTransitWarehouseIdAsync().ConfigureAwait(false);
            var directShipReceiveResult = await _purchaseOrderManager.ReceiveItemsAsync(new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
            {
                ReceivedByUserId = dto.ReceivedByUserId,
                ReceivedQuantity = receiveDirectShipQty,
                WarehouseId = directTransitWarehouseId,
                SellingPrice = null,
                ActualUnitCost = dto.ActualUnitCost
            });

            if (!physicalWarehouseRequired) createdGoodsReceiptId = directShipReceiveResult.CreatedGoodsReceiptId;
        }

        var receiveWarehouseQty = Math.Max(0, dto.ReceivedQuantity - receiveDirectShipQty);
        if (receiveWarehouseQty > 0)
        {
            var warehouseReceiveResult = await _purchaseOrderManager.ReceiveItemsAsync(new ReceivedGoodsForItemDto(purchaseOrder.Id, purchaseOrderItem.Id)
            {
                ReceivedByUserId = dto.ReceivedByUserId,
                ReceivedQuantity = receiveWarehouseQty,
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
                warehouseId = _warehouseDataReader.DataSource.Where(warehouse => warehouse.WarehouseType == WarehouseType.Physical).FirstOrDefault()?.Id;
            if (!warehouseId.HasValue)
                throw new OversupplyQuantityCannotHandledException();
            await _purchaseOrderManager.AcceptOversupplyToMainWarehouseAsync(
                dto.PurchaseOrderId, dto.PurchaseOrderItemId, oversupplyQty, warehouseId!.Value)
                .ConfigureAwait(false);
        }

        return ReceiveItemResultAppDto.CreateSuccess(dto.ReceivedQuantity, createdGoodsReceiptId);
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

        var createdGoodsReceiptIds = new List<Guid>();
        foreach (var line in lines)
        {
            var receiveResult = await ReceiveItemAsync(new ReceivedGoodsForItemAppDto(dto.PurchaseOrderId, line.PurchaseOrderItemId)
            {
                ReceivedByUserId = dto.ReceivedByUserId,
                ReceivedQuantity = line.ReceivedQuantity,
                WarehouseId = line.WarehouseId,
                ActualUnitCost = line.ActualUnitCost
            }).ConfigureAwait(false);

            if (!receiveResult.Success)
                return BulkReceiveGoodsResultAppDto.CreateError(receiveResult.ErrorMessage);

            if (receiveResult.CreatedGoodsReceiptId.HasValue)
                createdGoodsReceiptIds.Add(receiveResult.CreatedGoodsReceiptId.Value);
        }

        if (dto.AdditionalShipping > 0 || dto.AdditionalTax > 0)
        {
            var feeResult = await AddReceiptFeesAsync(dto.PurchaseOrderId, dto.AdditionalShipping, dto.AdditionalTax)
                .ConfigureAwait(false);

            if (!feeResult.Success)
                return BulkReceiveGoodsResultAppDto.CreateError(feeResult.ErrorMessage);
        }

        return BulkReceiveGoodsResultAppDto.CreateSuccess(createdGoodsReceiptIds);
    }

    public async Task<CommonActionResultDto> AddReceiptFeesAsync(Guid purchaseOrderId, decimal additionalShipping, decimal additionalTax)
    {
        await _purchaseOrderManager.AddReceiptFeesAsync(purchaseOrderId, additionalShipping, additionalTax)
            .ConfigureAwait(false);
        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<CommonActionResultDto> SetGoodsReceiptToPurchaseOrderAsync(SetGoodsReceiptToPurchaseOrderAppDto dto)
    {
        try
        {
            await _purchaseOrderManager.SetGoodsReceiptToPurchaseOrderAsync(
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
        await _purchaseOrderManager.RemoveGoodsReceiptFromPurchaseOrderAsync(
            new RemoveGoodsReceiptFromPurchaseOrderDto(dto.Id)).ConfigureAwait(false);
        return CommonActionResultDto.CreateSuccess();
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

        await _purchaseOrderManager.CancelAsync(id).ConfigureAwait(false);

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
        if (!purchaseOrder.CanAddItems)
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
        var domainDtos = await _purchaseOrderAllocationManager.GetEligibleOrderItemsForPoItemAsync(purchaseOrderItemId).ConfigureAwait(false);
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
        var domainDtos = await _purchaseOrderAllocationManager.GetNonDirectShipAllocationsForPoItemAsync(purchaseOrderItemId).ConfigureAwait(false);
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
        var allocation = _purchaseOrderItemAllocationDataReader.DataSource
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
        var allocations = _purchaseOrderItemAllocationDataReader.DataSource
            .Where(allocation => poItemIds.Any(poItemId => poItemId == allocation.PurchaseOrderItemId.SecondaryId)
                && allocation.Status != AllocationStatus.Cancelled)
            .OrderBy(allocation => allocation.CreatedOnUtc)
            .ToList();
        if (allocations.Count == 0)
            return [];

        var orderItemIds = allocations.Select(allocation => allocation.OrderItemId.SecondaryId).Distinct().ToList();
        var orderItems = _orderDataReader.DataSource
            .SelectMany(order => order.OrderItems
                .Where(item => orderItemIds.Contains(item.Id))
                .Select(item => new { Order = order, Item = item }))
            .ToDictionary(x => x.Item.Id);

        var customerIds = orderItems.Values.Select(x => x.Order.CustomerId).Distinct().ToList();
        var customers = _customerDataReader.DataSource
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

        var allocation = await _purchaseOrderAllocationManager
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
        await _purchaseOrderAllocationManager
            .ReleaseAllocationsOfPurchaseOrderItemAsync((dto.PurchaseOrderId, dto.PurchaseOrderItemId))
            .ConfigureAwait(false);

        return CommonActionResultDto.CreateSuccess();
    }

    public async Task<decimal> GetMaxAllocationQuantityForOrderItemAsync(Guid orderId, Guid orderItemId)
    {
        var order = await _orderDataReader.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(orderId);

        var orderItem = order.OrderItems.FirstOrDefault(item => item.Id == orderItemId);
        if (orderItem is null)
            throw new OrderItemIsNotFoundException();

        var allocatedOutstanding = _purchaseOrderItemAllocationDataReader.DataSource
            .Where(allocation => allocation.OrderItemId.SecondaryId == orderItemId && allocation.Status != AllocationStatus.Cancelled)
            .ToList()
            .Sum(allocation => Math.Max(0m, allocation.AllocatedQuantity - allocation.ReceivedQuantity));
        var activeDeliveryQuantity = _deliveryNoteDataReader.DataSource
            .Where(deliveryNote => deliveryNote.Status != DeliveryNoteStatus.Cancelled)
            .SelectMany(deliveryNote => deliveryNote.Items)
            .Where(item => item.OrderItemId == orderItemId)
            .Sum(item => item.Quantity);

        return Math.Max(0m, orderItem.Quantity - activeDeliveryQuantity - allocatedOutstanding);
    }
}

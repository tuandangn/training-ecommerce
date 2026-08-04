using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.Returns;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.GoodsReceipts;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.Users;
using Microsoft.EntityFrameworkCore;

namespace NamEcommerce.Domain.Services.PurchaseOrders;

public sealed class PurchaseOrderManager : IPurchaseOrderManager
{
    private readonly IRepository<PurchaseOrder> _purchaseOrderRepository;
    private readonly IEntityDataReader<PurchaseOrder> _purchaseOrderDataReader;
    private readonly IEntityDataReader<Vendor> _vendorOrderDataReader;
    private readonly IEntityDataReader<Warehouse> _warehouseOrderDataReader;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IGoodsReceiptManager _goodsReceiptManager;
    private readonly IRepository<GoodsReceipt> _goodsReceiptRepository;
    private readonly IEntityDataReader<GoodsReceipt> _goodsReceiptDataReader;
    private readonly IEntityDataReader<DeliveryNote> _deliveryNoteReader;
    private readonly IEntityDataReader<VendorReturn> _vendorReturnReader;
    private readonly IEntityDataReader<VendorDebt> _vendorDebtReader;
    private readonly IPurchaseOrderAllocationManager _purchaseOrderAllocationManager;
    private readonly IDirectShipManager _directShipManager;
    private readonly IVendorDebtManager _vendorDebtManager;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly EntityCodeGenerator _codeGenerator;

    public PurchaseOrderManager(IRepository<PurchaseOrder> poRepository, IEntityDataReader<PurchaseOrder> purchaseOrderDataReader,
        IEntityDataReader<Vendor> vendorOrderDataReader, IEntityDataReader<Warehouse> warehouseOrderDataReader,
        IEntityDataReader<Product> productDataReader,
        IGoodsReceiptManager goodsReceiptManager,
        IRepository<GoodsReceipt> goodsReceiptRepository,
        IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
        IEntityDataReader<DeliveryNote> deliveryNoteReader,
        IEntityDataReader<VendorReturn> vendorReturnReader,
        IEntityDataReader<VendorDebt> vendorDebtReader,
        IPurchaseOrderAllocationManager purchaseOrderAllocationManager,
        IDirectShipManager directShipManager,
        IVendorDebtManager vendorDebtManager,
        ICurrentUserAccessor currentUserAccessor,
        EntityCodeGenerator codeGenerator)
    {
        _purchaseOrderRepository = poRepository;
        _purchaseOrderDataReader = purchaseOrderDataReader;
        _vendorOrderDataReader = vendorOrderDataReader;
        _warehouseOrderDataReader = warehouseOrderDataReader;
        _productDataReader = productDataReader;
        _goodsReceiptManager = goodsReceiptManager;
        _goodsReceiptRepository = goodsReceiptRepository;
        _goodsReceiptDataReader = goodsReceiptDataReader;
        _deliveryNoteReader = deliveryNoteReader;
        _vendorReturnReader = vendorReturnReader;
        _vendorDebtReader = vendorDebtReader;
        _purchaseOrderAllocationManager = purchaseOrderAllocationManager;
        _directShipManager = directShipManager;
        _vendorDebtManager = vendorDebtManager;
        _currentUserAccessor = currentUserAccessor;
        _codeGenerator = codeGenerator;
    }

    public async Task<CreatePurchaseOrderResultDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var products = await _productDataReader.GetByIdsAsync(dto.Items.Select(item => item.ProductId).OfType<Guid>()).ConfigureAwait(false);
        var candidateVendorIds = products.SelectMany(p => p.ProductVendors).Select(v => v.VendorId).Distinct().ToList();
        var validVendorIds = candidateVendorIds.Where(vendorId => products.All(p => p.ProductVendors.Any(v => v.VendorId == vendorId))).ToList();
        if (validVendorIds.Count == 0)
            throw new PurchaseOrderNoVendorsAppropriateException();

        if (!validVendorIds.Contains(dto.VendorId))
            throw new PurchaseOrderVendorIsNotAppropriateException();

        var purchaseOrder = await CreatePurchaseOrderAggregateAsync(
            dto.VendorId,
            dto.WarehouseId,
            dto.PlacedOnUtc,
            dto.ExpectedDeliveryDateUtc,
            dto.Note,
            dto.TaxAmount,
            dto.ShippingAmount).ConfigureAwait(false);
        foreach (var orderItem in dto.Items)
        {
            await purchaseOrder.AddPurchaseOrderItemAsync(new PurchaseOrderItem(purchaseOrder.Id, orderItem.ProductId, orderItem.QuantityOrdered, orderItem.UnitCost)
            {
                Note = orderItem.Note
            }, _productDataReader).ConfigureAwait(false);
        }

        purchaseOrder.MarkCreated();
        var insertedPurchaseOrder = await _purchaseOrderRepository.InsertAsync(purchaseOrder).ConfigureAwait(false);

        return new CreatePurchaseOrderResultDto
        {
            CreatedId = insertedPurchaseOrder.Id
        };
    }

    public async Task<PurchaseOrderQuickCreateResultDto> QuickCreatePurchaseOrderAsync(PurchaseOrderQuickCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var createPurchaseOrderDto = new CreatePurchaseOrderDto
        {
            PlacedOnUtc = dto.PlacedOnUtc,
            Note = dto.Note,
            VendorId = dto.VendorId,
            WarehouseId = dto.DefaultWarehouseId
        };
        foreach (var item in dto.Items)
        {
            createPurchaseOrderDto.Items.Add(new CreatePurchaseOrderDto.CreatedPurchaseOrderItemDto
            {
                ProductId = item.ProductId,
                QuantityOrdered = item.Quantity,
                UnitCost = item.UnitCost ?? 0
            });
        }
        var insertResult = await CreatePurchaseOrderAsync(createPurchaseOrderDto).ConfigureAwait(false);

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(insertResult.CreatedId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(insertResult.CreatedId);

        var currentUser = await _currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var userId = currentUser?.Id;

        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Submitted, userId);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;
        purchaseOrder.MarkStatusChanged(PurchaseOrderStatus.Draft);
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        purchaseOrder.ChangeStatus(PurchaseOrderStatus.Approved, userId);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;
        purchaseOrder.MarkStatusChanged(PurchaseOrderStatus.Submitted);
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        var createdReceiptIds = new List<Guid>();
        if (dto.IsReceived)
        {

            var insertedItems = purchaseOrder.Items.ToList();
            var lines = dto.Items.Select((item, i) => new BulkReceiveGoodsForPurchaseOrderLineDto
            {
                PurchaseOrderItemId = insertedItems[i].Id,
                WarehouseId = dto.DefaultWarehouseId!.Value,
                ReceivedQuantity = item.Quantity,
                ActualUnitCost = item.UnitCost
            }).ToList();

            var receiveResult = await BulkReceiveItemsAsync(new BulkReceiveGoodsForPurchaseOrderDto(purchaseOrder.Id)
            {
                ReceivedOnUtc = dto.ReceivedOnUtc,
                Lines = lines,
                ReceivedByUserId = userId
            }).ConfigureAwait(false);

            createdReceiptIds.AddRange(receiveResult.CreatedGoodsReceiptIds);
        }

        if (dto.IsPaid)
        {
            await _vendorDebtManager.RecordFlexiblePaymentForVendorAsync(new CreateVendorPaymentDto
            {
                VendorId = dto.VendorId,
                PurchaseOrderId = purchaseOrder.Id,
                Amount = dto.Payment!.PaidAmount,
                PaymentMethod = dto.Payment.PaymentMethod,
                PaymentType = dto.Payment.PaymentType,
                PaidOnUtc = dto.ReceivedOnUtc ?? dto.PlacedOnUtc
            }).ConfigureAwait(false);
        }

        return new PurchaseOrderQuickCreateResultDto
        {
            PurchaseOrderId = purchaseOrder.Id
        };
    }

    public async Task<CreatePurchaseOrderResultDto> CopyPurchaseOrderAsync(Guid sourceId)
    {
        var source = await _purchaseOrderRepository.GetByIdAsync(sourceId).ConfigureAwait(false);
        if (source is null)
            throw new PurchaseOrderIsNotFoundException(sourceId);

        var now = DateTime.UtcNow;
        var po = await CreatePurchaseOrderAggregateAsync(
            source.VendorId,
            source.WarehouseId,
            now,
            source.ExpectedDeliveryDateUtc > now ? source.ExpectedDeliveryDateUtc : null,
            source.Note,
            0, 0).ConfigureAwait(false);

        foreach (var item in source.Items)
        {
            await po.AddPurchaseOrderItemAsync(
                new PurchaseOrderItem(po.Id, item.ProductId, item.QuantityOrdered, item.UnitCost) { Note = item.Note },
                _productDataReader, requireVendorProduct: false).ConfigureAwait(false);
        }

        po.MarkCreated();
        var inserted = await _purchaseOrderRepository.InsertAsync(po).ConfigureAwait(false);
        return new CreatePurchaseOrderResultDto { CreatedId = inserted.Id };
    }

    public async Task<CreatePurchaseOrderResultDto> SplitPurchaseOrderAsync(SplitPurchaseOrderDto dto)
    {
        var source = await _purchaseOrderRepository.GetByIdAsync(dto.SourcePurchaseOrderId).ConfigureAwait(false);
        if (source is null)
            throw new PurchaseOrderIsNotFoundException(dto.SourcePurchaseOrderId);

        if (!source.CanUpdateItems())
            throw new PurchaseOrderCannotUpdateOrderItemsException();

        if (dto.Items.Count == 0)
            throw new PurchaseOrderDataIsInvalidException("Error.PurchaseOrder.SplitItemsRequired");

        foreach (var split in dto.Items)
        {
            var srcItem = source.Items.FirstOrDefault(i => i.Id == split.ItemId)
                ?? throw new PurchaseOrderItemIsNotFoundException();

            var available = srcItem.QuantityOrdered - srcItem.QuantityReceived;
            if (split.Quantity <= 0 || split.Quantity > available)
                throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemQuantityMustBePositive");
        }

        var hasRemainingInSource = source.Items.Any(srcItem =>
        {
            var splitItem = dto.Items.FirstOrDefault(s => s.ItemId == srcItem.Id);
            if (splitItem is null) return true;
            return srcItem.QuantityOrdered - splitItem.Quantity > 0;
        });

        if (!hasRemainingInSource)
            throw new PurchaseOrderDataIsInvalidException("Error.PurchaseOrder.SplitMustLeaveItemsInSource");

        var now = DateTime.UtcNow;
        var po2 = await CreatePurchaseOrderAggregateAsync(
            source.VendorId,
            source.WarehouseId,
            now,
            source.ExpectedDeliveryDateUtc > now ? source.ExpectedDeliveryDateUtc : null,
            source.Note,
            0, 0).ConfigureAwait(false);

        foreach (var split in dto.Items)
        {
            var srcItem = source.Items.First(i => i.Id == split.ItemId);
            await po2.AddPurchaseOrderItemAsync(
                new PurchaseOrderItem(po2.Id, srcItem.ProductId, split.Quantity, srcItem.UnitCost) { Note = srcItem.Note },
                _productDataReader, requireVendorProduct: false).ConfigureAwait(false);
        }

        po2.MarkCreated();
        await _purchaseOrderRepository.InsertAsync(po2).ConfigureAwait(false);

        foreach (var split in dto.Items)
        {
            var srcItem = source.Items.First(i => i.Id == split.ItemId);
            var remaining = srcItem.QuantityOrdered - split.Quantity;
            if (remaining <= 0)
                source.RemoveOrderItem(split.ItemId);
            else
                source.UpdateOrderItem(split.ItemId, remaining, srcItem.UnitCost, srcItem.Note);
        }

        source.UpdatedOnUtc = DateTime.UtcNow;
        source.MarkUpdated();
        await _purchaseOrderRepository.UpdateAsync(source).ConfigureAwait(false);

        return new CreatePurchaseOrderResultDto { CreatedId = po2.Id };
    }

    public async Task<ExistingDraftPurchaseOrderDto?> FindDraftForVendorAsync(Guid vendorId)
    {
        if (vendorId == Guid.Empty)
            throw new VendorIsNotFoundException(vendorId);

        var draft = await _purchaseOrderDataReader.DataSource
            .Where(purchaseOrder => purchaseOrder.VendorId == vendorId && purchaseOrder.Status == PurchaseOrderStatus.Draft)
            .OrderByDescending(purchaseOrder => purchaseOrder.CreatedOnUtc)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        if (draft is null)
            return null;

        return new ExistingDraftPurchaseOrderDto(draft.Id)
        {
            VendorId = draft.VendorId,
            Code = draft.Code,
            CreatedOnUtc = draft.CreatedOnUtc,
            ExpectedDeliveryDateUtc = draft.ExpectedDeliveryDateUtc
        };
    }

    public async Task<IList<RelatedPurchaseOrderDto>> FindRelatedPurchaseOrdersAsync(Guid vendorId, IList<Guid> productIds, IList<PurchaseOrderStatus> statuses)
    {
        if (vendorId == Guid.Empty)
            throw new VendorIsNotFoundException(vendorId);

        var vendor = await _vendorOrderDataReader.GetByIdAsync(vendorId).ConfigureAwait(false);
        if (vendor is null)
            throw new VendorIsNotFoundException(vendorId);

        var productIdSet = productIds
            .Where(productId => productId != Guid.Empty)
            .Distinct()
            .ToHashSet();
        if (productIdSet.Count == 0)
            return [];

        var statusSet = statuses.Count == 0
            ? new HashSet<PurchaseOrderStatus>
            {
                PurchaseOrderStatus.Draft,
                PurchaseOrderStatus.Submitted,
                PurchaseOrderStatus.Approved
            }
            : statuses.Distinct().ToHashSet();

        var products = await _productDataReader.DataSource
            .Where(product => productIdSet.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, product => product.Name).ConfigureAwait(false);

        var purchaseOrders = await _purchaseOrderDataReader.DataSource
            .Where(purchaseOrder => purchaseOrder.VendorId == vendorId && statusSet.Contains(purchaseOrder.Status))
            .OrderByDescending(purchaseOrder => purchaseOrder.CreatedOnUtc)
            .ToListAsync().ConfigureAwait(false);

        var result = new List<RelatedPurchaseOrderDto>();
        foreach (var purchaseOrder in purchaseOrders)
        {
            var items = new List<RelatedPurchaseOrderItemDto>();
            foreach (var item in purchaseOrder.Items.Where(item => productIdSet.Contains(item.ProductId)))
            {
                ArgumentNullException.ThrowIfNull(_purchaseOrderAllocationManager);
                var allocations = await _purchaseOrderAllocationManager.GetAllocationsForPurchaseOrderItemAsync(item.Id).ConfigureAwait(false);
                var allocatedQuantity = allocations.Sum(allocation => allocation.AllocatedQuantity);
                var availableForAllocation = Math.Max(0, item.QuantityOrdered - allocatedQuantity);

                items.Add(new RelatedPurchaseOrderItemDto
                {
                    PurchaseOrderItemId = item.Id,
                    ProductId = item.ProductId,
                    ProductName = products.TryGetValue(item.ProductId, out var productName) ? productName : string.Empty,
                    QuantityOrdered = item.QuantityOrdered,
                    AllocatedQuantity = allocatedQuantity,
                    ReceivedQuantity = item.QuantityReceived,
                    AvailableForAllocation = availableForAllocation,
                    UnitCost = item.UnitCost
                });
            }

            var canMerge = purchaseOrder.Status == PurchaseOrderStatus.Draft;
            var canAllocate = purchaseOrder.Status is PurchaseOrderStatus.Draft
                or PurchaseOrderStatus.Submitted
                or PurchaseOrderStatus.Approved;
            if (!canMerge && items.All(item => item.AvailableForAllocation <= 0))
                continue;

            result.Add(new RelatedPurchaseOrderDto(purchaseOrder.Id)
            {
                VendorId = purchaseOrder.VendorId,
                VendorName = vendor.Name,
                Code = purchaseOrder.Code,
                Status = purchaseOrder.Status,
                CreatedOnUtc = purchaseOrder.CreatedOnUtc,
                ExpectedDeliveryDateUtc = purchaseOrder.ExpectedDeliveryDateUtc,
                TotalAmount = purchaseOrder.TotalAmount,
                ItemCount = purchaseOrder.Items.Count(),
                CanAllocate = canAllocate,
                CanMerge = canMerge,
                Items = items
            });
        }

        return result;
    }

    public async Task<CreatePoFromShortageResultDto> CreatePurchaseOrderFromShortageAsync(CreatePoFromShortageDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var createPurchaseOrderDto = new CreatePurchaseOrderDto
        {
            PlacedOnUtc = DateTime.UtcNow,
            VendorId = dto.VendorId,
            WarehouseId = dto.WarehouseId,
            ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc,
            Note = dto.Note
        };
        foreach (var item in dto.Items)
        {
            createPurchaseOrderDto.Items.Add(new CreatePurchaseOrderDto.CreatedPurchaseOrderItemDto
            {
                ProductId = item.ProductId,
                QuantityOrdered = item.Quantity,
                UnitCost = item.UnitCost,
                Note = item.Note
            });
        }

        var createResult = await CreatePurchaseOrderAsync(createPurchaseOrderDto).ConfigureAwait(false);
        // Repository.GetByIdAsync tìm được PO vừa stage (chưa save) trong ChangeTracker — reader query DB sẽ không thấy
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(createResult.CreatedId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(createResult.CreatedId);

        var allocationSources = MapCreatedPurchaseOrderItemsForShortage(purchaseOrder, dto.Items);
        await AllocateShortageItemsAsync(allocationSources).ConfigureAwait(false);

        return new CreatePoFromShortageResultDto
        {
            PurchaseOrderId = purchaseOrder.Id,
            PurchaseOrderCode = purchaseOrder.Code,
            CreatedNew = true
        };
    }

    public async Task<CreatePoFromShortageResultDto> AddItemsToExistingDraftAsync(Guid purchaseOrderId, IList<CreatePoFromShortageItemDto> items)
    {
        if (items.Count == 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemRequired");

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);
        if (purchaseOrder.Status != PurchaseOrderStatus.Draft)
            throw new PurchaseOrderCannotUpdateOrderItemsException();

        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;
        purchaseOrder.MarkUpdated();
        var updatedPurchaseOrder = await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        var allocationSources = await AddOrMergeShortageItemsAsync(purchaseOrder, items).ConfigureAwait(false);
        foreach (var allocationSource in allocationSources.Where(source => source.IsNew))
            purchaseOrder.MarkItemAdded(allocationSource.PurchaseOrderItem);

        await AllocateShortageItemsAsync(allocationSources).ConfigureAwait(false);

        return new CreatePoFromShortageResultDto
        {
            PurchaseOrderId = updatedPurchaseOrder.Id,
            PurchaseOrderCode = updatedPurchaseOrder.Code,
            CreatedNew = false
        };
    }

    public async Task<UpdatePurchaseOrderResultDto> UpdatePurchaseOrderAsync(UpdatePurchaseOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(dto.Id);

        var vendor = await _vendorOrderDataReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor is null)
            throw new VendorIsNotFoundException(dto.VendorId);

        if (dto.ExpectedDeliveryDateUtc < DateTime.UtcNow && dto.ExpectedDeliveryDateUtc != purchaseOrder.ExpectedDeliveryDateUtc)
            throw new PurchaseOrderDataIsInvalidException("Error.ExpectedDeliveryDateCannotBeInPast");

        if (dto.WarehouseId.HasValue)
        {
            var warehouse = await _warehouseOrderDataReader.GetByIdAsync(dto.WarehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
                throw new WarehouseIsNotFoundException(dto.WarehouseId.Value);
        }

        purchaseOrder.SetPlacedDate(dto.PlacedOnUtc);
        purchaseOrder.Note = dto.Note;
        purchaseOrder.ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc;
        purchaseOrder.ShippingAmount = dto.ShippingAmount;
        purchaseOrder.TaxAmount = dto.TaxAmount;
        await purchaseOrder.ChangeVendorAsync(dto.VendorId, _vendorOrderDataReader).ConfigureAwait(false);
        await purchaseOrder.ChangeWarehouse(dto.WarehouseId, _warehouseOrderDataReader).ConfigureAwait(false);

        purchaseOrder.MarkUpdated();
        var updatedPurchaseOrder = await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        return new UpdatePurchaseOrderResultDto(updatedPurchaseOrder.Id)
        {
            PlacedOnUtc = updatedPurchaseOrder.PlacedOnUtc,
            VendorId = updatedPurchaseOrder.VendorId,
            WarehouseId = updatedPurchaseOrder.WarehouseId,
            TaxAmount = updatedPurchaseOrder.TaxAmount,
            ShippingAmount = updatedPurchaseOrder.ShippingAmount,
            Note = updatedPurchaseOrder.Note,
            ExpectedDeliveryDateUtc = updatedPurchaseOrder.ExpectedDeliveryDateUtc
        };
    }

    public async Task<AddPurchaseOrderItemResultDto> AddPurchaseOrderItemAsync(AddPurchaseOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(dto.PurchaseOrderId);

        var product = await _productDataReader.GetByIdAsync(dto.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(dto.ProductId);

        if (!purchaseOrder.CanUpdateItems())
            throw new PurchaseOrderCannotAddItemException();

        var purchaseOrderItem = new PurchaseOrderItem(dto.PurchaseOrderId, dto.ProductId, dto.QuantityOrdered, dto.UnitCost)
        {
            Note = dto.Note,
        };
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, _productDataReader).ConfigureAwait(false);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        purchaseOrder.MarkItemAdded(purchaseOrderItem, product.Name);

        return new AddPurchaseOrderItemResultDto
        {
            PurchaseOrderId = purchaseOrder.Id,
            CreatedItemId = purchaseOrderItem.Id
        };
    }

    public async Task UpdatePurchaseOrderItemAsync(UpdatePurchaseOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(dto.PurchaseOrderId);

        if (!purchaseOrder.CanUpdateItems())
            throw new PurchaseOrderCannotUpdateOrderItemsException();

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == dto.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
            throw new PurchaseOrderItemIsNotFoundException();

        var product = await _productDataReader.GetByIdAsync(purchaseOrderItem.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(purchaseOrderItem.ProductId);

        var oldQuantityOrdered = purchaseOrderItem.QuantityOrdered;
        var oldUnitCost = purchaseOrderItem.UnitCost;
        var oldNote = purchaseOrderItem.Note;

        purchaseOrder.UpdateOrderItem(dto.PurchaseOrderItemId, dto.QuantityOrdered, dto.UnitCost, dto.Note);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;
        purchaseOrder.MarkItemUpdated(purchaseOrderItem, oldQuantityOrdered, oldUnitCost, oldNote, product.Name);

        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }

    public async Task ClosePartialAsync(Guid purchaseOrderId, string reason)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        if (string.IsNullOrWhiteSpace(reason))
            throw new PurchaseOrderDataIsInvalidException("Error.PurchaseOrder.CloseReasonRequired");
        if (purchaseOrder.Status != PurchaseOrderStatus.Receiving)
            throw new PurchaseOrderCannotChangeStatusException();

        foreach (var item in purchaseOrder.Items)
            await _purchaseOrderAllocationManager
                .ReleaseAllocationsOfPurchaseOrderItemAsync((purchaseOrder.Id, item.Id))
                .ConfigureAwait(false);

        var oldStatus = purchaseOrder.Status;
        purchaseOrder.ClosePartial(reason);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        purchaseOrder.MarkStatusChanged(oldStatus);
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }

    public async Task ChangeStatusAsync(Guid purchaseOrderId, PurchaseOrderStatus status)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        if (!purchaseOrder.CanChangeStatusTo(status))
            throw new PurchaseOrderCannotChangeStatusException();

        var oldStatus = purchaseOrder.Status;
        var currentUser = await _currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        purchaseOrder.ChangeStatus(status, currentUser?.Id);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        purchaseOrder.MarkStatusChanged(oldStatus);
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }

    public async Task<bool> CanChangeStatusToAsync(Guid purchaseOrderId, PurchaseOrderStatus status)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        return purchaseOrder.CanChangeStatusTo(status);
    }

    public async Task<bool> CanAddPurchaseOrderItemsAsync(Guid purchaseOrderId)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        return purchaseOrder.CanUpdateItems();
    }

    public Task<bool> DoesCodeExistAsync(string code, Guid? comparesWithCurrentId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);

        var query = from purchaseOrder in _purchaseOrderDataReader.DataSource
                    where purchaseOrder.Code == code && (comparesWithCurrentId == null || purchaseOrder.Id != comparesWithCurrentId)
                    select purchaseOrder;

        return query.AnyAsync();
    }

    public async Task<ReceivedGoodsForItemResultDto> ReceiveItemsAsync(ReceivedGoodsForItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(dto.PurchaseOrderId);

        return await ReceiveSingleItemAsync(purchaseOrder, dto).ConfigureAwait(false);
    }

    private async Task<ReceivedGoodsForItemResultDto> ReceiveSingleItemAsync(PurchaseOrder purchaseOrder, ReceivedGoodsForItemDto dto)
    {
        if (!purchaseOrder.CanReceiveGoods())
            throw new PurchaseOrderCannotReceiveGoodsException();

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == dto.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
            throw new PurchaseOrderItemIsNotFoundException();

        var product = await _productDataReader.GetByIdAsync(purchaseOrderItem.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(purchaseOrderItem.ProductId);

        Guid? warehouseId = dto.WarehouseId ?? purchaseOrder.WarehouseId;
        if (!warehouseId.HasValue)
            throw new ArgumentException("Warehouse is required", nameof(dto));
        else
        {
            var warehouse = await _warehouseOrderDataReader.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
                throw new WarehouseIsNotFoundException(warehouseId.Value);
        }

        var hasReceivableDirectShip = await _directShipManager
            .HasReceivableDirectShipAllocationsAsync(purchaseOrderItem.Id).ConfigureAwait(false);
        if (hasReceivableDirectShip)
            await EnsureDirectShipTransitWarehouseConfiguredAsync().ConfigureAwait(false);

        purchaseOrderItem.AddQuantityReceived(dto.ReceivedQuantity);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        purchaseOrder.MarkItemReceived(purchaseOrderItem.Id, dto.ReceivedQuantity);

        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        var grResult = await _goodsReceiptManager.CreateFromPurchaseOrderReceivingAsync(new CreateGoodsReceiptFromPurchaseOrderDto
        {
            ReceivedOnUtc = dto.ReceivedOnUtc,
            PurchaseOrderId = purchaseOrder.Id,
            PurchaseOrderCode = purchaseOrder.Code,
            VendorId = purchaseOrder.VendorId,
            ProductId = purchaseOrderItem.ProductId,
            WarehouseId = warehouseId.Value,
            Quantity = dto.ReceivedQuantity,
            UnitCost = dto.ActualUnitCost ?? purchaseOrderItem.UnitCost
        }).ConfigureAwait(false);

        var distributeResult = await _purchaseOrderAllocationManager
            .SyncReceivedForPurchaseOrderItemAsync(purchaseOrderItem.Id, purchaseOrderItem.QuantityReceived)
            .ConfigureAwait(false);
        await ProcessDirectShipReceiptsAsync(
            distributeResult.DirectShipReceipts,
            grResult.CreatedId,
            warehouseId.Value).ConfigureAwait(false);

        return new ReceivedGoodsForItemResultDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            ReceivedQuantity = dto.ReceivedQuantity,
            CreatedGoodsReceiptId = grResult.CreatedId
        };
    }

    public async Task<BulkReceiveGoodsForPurchaseOrderResultDto> BulkReceiveItemsAsync(BulkReceiveGoodsForPurchaseOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(dto.PurchaseOrderId);

        if (!purchaseOrder.CanReceiveGoods())
            throw new PurchaseOrderCannotReceiveGoodsException();

        var receivingPurchaseOrderItemIds = dto.Lines
            .Select(line => line.PurchaseOrderItemId)
            .Distinct()
            .ToList();
        var hasReceivableDirectShip = false;
        foreach (var itemId in receivingPurchaseOrderItemIds)
        {
            hasReceivableDirectShip = await _directShipManager.HasReceivableDirectShipAllocationsAsync(itemId)
                .ConfigureAwait(false);
            if (hasReceivableDirectShip)
                break;
        }

        if (hasReceivableDirectShip)
            await EnsureDirectShipTransitWarehouseConfiguredAsync().ConfigureAwait(false);

        // Aggregate-validate qty theo PO item (cùng item nhiều kho phải cộng dồn trước khi check).
        var groupedByItem = dto.Lines.GroupBy(line => line.PurchaseOrderItemId);
        foreach (var group in groupedByItem)
        {
            var poItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == group.Key);
            if (poItem is null)
                throw new PurchaseOrderItemIsNotFoundException();

            var totalReceiving = group.Sum(x => x.ReceivedQuantity);
            if (poItem.QuantityReceived + totalReceiving > poItem.QuantityOrdered)
                throw new PurchaseOrderReceiveQuantityExceedsOrderedQuantityException();
        }

        // Validate warehouse cho tất cả lines (cache theo Id).
        var warehouseCache = new Dictionary<Guid, Warehouse?>();
        var createdReceiptIds = new List<Guid>();
        foreach (var line in dto.Lines)
        {
            if (!warehouseCache.TryGetValue(line.WarehouseId, out var warehouse))
            {
                warehouse = await _warehouseOrderDataReader.GetByIdAsync(line.WarehouseId).ConfigureAwait(false);
                warehouseCache[line.WarehouseId] = warehouse;
            }
            if (warehouse is null)
                throw new WarehouseIsNotFoundException(line.WarehouseId);
        }

        foreach (var line in dto.Lines)
        {
            var result = await ReceiveItemsAsync(new ReceivedGoodsForItemDto(purchaseOrder.Id, line.PurchaseOrderItemId)
            {
                ReceivedOnUtc = dto.ReceivedOnUtc,
                ReceivedByUserId = dto.ReceivedByUserId,
                ReceivedQuantity = line.ReceivedQuantity,
                WarehouseId = line.WarehouseId,
                ActualUnitCost = line.ActualUnitCost
            }).ConfigureAwait(false);

            if (result.CreatedGoodsReceiptId.HasValue)
                createdReceiptIds.Add(result.CreatedGoodsReceiptId.Value);
        }

        return new BulkReceiveGoodsForPurchaseOrderResultDto
        {
            PurchaseOrderId = purchaseOrder.Id,
            CreatedGoodsReceiptIds = createdReceiptIds
        };
    }

    public async Task SetGoodsReceiptToPurchaseOrderAsync(SetGoodsReceiptToPurchaseOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var goodsReceipt = await _goodsReceiptRepository.GetByIdAsync(dto.Id).ConfigureAwait(false)
            ?? throw new GoodsReceiptIsNotFoundException(dto.Id);

        if (goodsReceipt.PurchaseOrderId.HasValue)
            throw new GoodsReceiptCannotSetToPurchaseOrderException();

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false)
            ?? throw new PurchaseOrderIsNotFoundException(dto.PurchaseOrderId);

        if (!purchaseOrder.CanReceiveGoods())
            throw new PurchaseOrderCannotReceiveGoodsException();

        var grBuckets = BuildGoodsReceiptBuckets(goodsReceipt);
        var poBuckets = BuildPurchaseOrderBuckets(purchaseOrder);
        var resolvePoItems = new List<(Guid itemId, decimal qty)>();
        var needUpdateUnitCostItems = new List<(Guid grItemId, decimal qty, decimal unitCost)>();

        ResolveCostedItems(grBuckets, poBuckets, resolvePoItems);
        ResolveUncostedItems(grBuckets, poBuckets, resolvePoItems, needUpdateUnitCostItems);
        ApplyCostAssignmentsAndSplit(goodsReceipt, purchaseOrder.Id, needUpdateUnitCostItems);

        goodsReceipt.SetToPurchaseOrder(purchaseOrder.Id, purchaseOrder.Code);
        await _goodsReceiptRepository.UpdateAsync(goodsReceipt).ConfigureAwait(false);

        foreach (var (itemId, qty) in resolvePoItems)
        {
            var purchaseOrderItem = purchaseOrder.Items.First(item => item.Id == itemId);
            purchaseOrderItem.AddQuantityReceived(qty);
        }

        purchaseOrder.VerifyStatus();
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        foreach (var itemId in resolvePoItems.Select(item => item.itemId).Distinct())
        {
            var purchaseOrderItem = purchaseOrder.Items.First(item => item.Id == itemId);
            var distributeResult = await _purchaseOrderAllocationManager
                .SyncReceivedForPurchaseOrderItemAsync(itemId, purchaseOrderItem.QuantityReceived)
                .ConfigureAwait(false);

            var sourceWarehouseId = goodsReceipt.Items
                .FirstOrDefault(item => item.ProductId == purchaseOrderItem.ProductId && item.WarehouseId.HasValue)
                ?.WarehouseId;
            if (!sourceWarehouseId.HasValue && distributeResult.DirectShipReceipts.Count > 0)
                throw new WarehouseIsRequiredException();

            foreach (var receipt in distributeResult.DirectShipReceipts)
            {
                await _directShipManager.OnAllocationReceivedAsync(
                    receipt.AllocationId,
                    receipt.Quantity,
                    goodsReceipt.Id,
                    sourceWarehouseId!.Value).ConfigureAwait(false);
            }
        }
    }

    public async Task RemoveGoodsReceiptFromPurchaseOrderAsync(RemoveGoodsReceiptFromPurchaseOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var goodsReceipt = await _goodsReceiptRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (goodsReceipt is null)
            throw new GoodsReceiptIsNotFoundException(dto.Id);

        if (!goodsReceipt.PurchaseOrderId.HasValue)
            return;

        var purchaseOrderId = goodsReceipt.PurchaseOrderId.Value;
        var hasActiveDirectShipDelivery = _deliveryNoteReader.DataSource
            .Any(note => note.SourceGoodsReceiptId == dto.Id && note.Status != DeliveryNoteStatus.Cancelled);
        if (hasActiveDirectShipDelivery)
            throw new GoodsReceiptCannotRemoveDueToDirectShipDeliveryException(dto.Id);

        var hasConfirmedReturns = _vendorReturnReader.DataSource
            .Any(r => r.GoodsReceiptId == dto.Id && r.Status == VendorReturnStatus.Confirmed);
        if (hasConfirmedReturns)
            throw new GoodsReceiptHasConfirmedReturnsException(dto.Id);

        var linkedDebt = _vendorDebtReader.DataSource
            .FirstOrDefault(d => d.GoodsReceiptId == dto.Id);
        if (linkedDebt is not null && (linkedDebt.PaidAmount > 0 || linkedDebt.RemainingAmount != linkedDebt.TotalAmount))
            throw new GoodsReceiptCannotDeleteDueToTouchedDebtException(dto.Id);

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        if (purchaseOrder.Status != PurchaseOrderStatus.Approved && purchaseOrder.Status != PurchaseOrderStatus.Receiving)
            throw new PurchaseOrderCannotReceiveGoodsException();

        var revertByProductCost = goodsReceipt.Items
            .GroupBy(i => (i.ProductId, i.UnitCost))
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        var changedPurchaseOrderItemIds = new HashSet<Guid>();

        foreach (var ((productId, unitCost), totalQty) in revertByProductCost)
        {
            var remaining = totalQty;
            var candidatePoItems = purchaseOrder.Items
                .Where(po => po.ProductId == productId
                    && (!unitCost.HasValue || po.UnitCost == unitCost.Value)
                    && po.QuantityReceived > 0)
                .OrderByDescending(po => po.QuantityReceived)
                .ToList();

            foreach (var poItem in candidatePoItems)
            {
                if (remaining <= 0) break;
                var revertQty = Math.Min(poItem.QuantityReceived, remaining);
                poItem.RevertQuantityReceived(revertQty);
                changedPurchaseOrderItemIds.Add(poItem.Id);
                remaining -= revertQty;
            }
        }

        if (linkedDebt is not null)
            await _vendorDebtManager.DeleteDebtFromGoodsReceiptAsync(dto.Id).ConfigureAwait(false);

        goodsReceipt.RemoveFromPurchaseOrder();
        await _goodsReceiptRepository.UpdateAsync(goodsReceipt).ConfigureAwait(false);

        purchaseOrder.RevertReceivingIfEmpty();
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        foreach (var itemId in changedPurchaseOrderItemIds)
        {
            var purchaseOrderItem = purchaseOrder.Items.First(item => item.Id == itemId);
            await _purchaseOrderAllocationManager
                .SyncReceivedForPurchaseOrderItemAsync(itemId, purchaseOrderItem.QuantityReceived)
                .ConfigureAwait(false);
        }
    }

    private async Task EnsureDirectShipTransitWarehouseConfiguredAsync()
    {
        var transitWarehouse = await _warehouseOrderDataReader.DataSource
            .FirstOrDefaultAsync(w => w.WarehouseType == WarehouseType.DirectTransit)
            .ConfigureAwait(false);

        if (transitWarehouse is null)
            throw new DirectShipTransitWarehouseNotConfiguredException();
    }

    private async Task ProcessDirectShipReceiptsAsync(IReadOnlyList<AllocationReceiptDto> receipts, Guid sourceGoodsReceiptId, Guid receivedWarehouseId)
    {
        foreach (var receipt in receipts.Where(receipt => receipt.IsDirectShip))
        {
            await _directShipManager.OnAllocationReceivedAsync(
                receipt.AllocationId,
                receipt.Quantity,
                sourceGoodsReceiptId,
                receivedWarehouseId).ConfigureAwait(false);
        }
    }

    public async Task VerifyStatusAsync(Guid purchaseOrderId)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        var hasChanged = purchaseOrder.VerifyStatus();

        if (!hasChanged)
            return;

        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }

    public async Task<IPagedDataDto<PurchaseOrderDto>> GetPurchaseOrdersAsync(int pageIndex, int pageSize, string? keywords, PurchaseOrderStatus? status)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0, nameof(pageIndex));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0, nameof(pageSize));

        var query = _purchaseOrderDataReader.DataSource;

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (!string.IsNullOrEmpty(keywords))
        {
            var normalizedKeywords = TextHelper.Normalize(keywords);
            var uppercaseKeywords = keywords.Trim().ToUpper();

            var vendorIds = await _vendorOrderDataReader.DataSource
                .Where(v => v.Name.ToUpper().Contains(uppercaseKeywords) || v.Name.ToUpper().Contains(normalizedKeywords) || v.NormalizedName.Contains(normalizedKeywords))
                .Select(v => v.Id)
                .OfType<Guid?>()
                .ToListAsync().ConfigureAwait(false);
            IList<Guid?> warehouseIds = [];
            IList<Guid?> userIds = [];

            query = query.Where(c => c.Code.Contains(keywords)
                || vendorIds.Contains(c.VendorId)
                || warehouseIds.Contains(c.WarehouseId) || c.Items.Any(item => warehouseIds.Contains(item.WarehouseId))
                || userIds.Contains(c.CreatedByUserId));
        }

        query = query.OrderByDescending(c => c.CreatedOnUtc);

        var totalCount = await query.CountAsync().ConfigureAwait(false);
        var pagedData = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync().ConfigureAwait(false);

        var data = PagedDataDto.Create(pagedData.Select(purchaseOrder => purchaseOrder.ToDto()), pageIndex, pageSize, totalCount);
        return data;
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(id);
        if (purchaseOrder is null)
            return null;

        return purchaseOrder.ToDto();
    }

    public async Task<bool> CanReceiveGoodsAsync(Guid purchaseOrderId)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        return purchaseOrder.CanReceiveGoods();
    }

    public async Task DeleteOrderItemAsync(Guid purchaseOrderId, Guid itemId)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        if (!purchaseOrder.CanUpdateItems())
            throw new PurchaseOrderCannotUpdateOrderItemsException();

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == itemId);
        if (purchaseOrderItem is null)
            throw new PurchaseOrderItemIsNotFoundException();

        var product = await _productDataReader.GetByIdAsync(purchaseOrderItem.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(purchaseOrderItem.ProductId);

        purchaseOrder.MarkItemRemoved(purchaseOrderItem, product.Name);
        purchaseOrder.RemoveOrderItem(itemId);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }

    public async Task<IList<RecentPurchasePriceDto>> GetRecentPurchasePricesAsync(Guid productId)
    {
        // Lấy tất cả đơn nhập (không bị hủy) có chứa sản phẩm này
        var purchaseOrders = await _purchaseOrderDataReader.DataSource
            .Where(po => po.Status != PurchaseOrderStatus.Cancelled
                      && po.Items.Any(item => item.ProductId == productId))
            .OrderByDescending(po => po.PlacedOnUtc)
            .ToListAsync().ConfigureAwait(false);

        // Gom nhóm theo VendorId, lấy lần nhập gần nhất của mỗi nhà cung cấp
        var groupedByVendor = purchaseOrders
            .SelectMany(po => po.Items
                .Where(item => item.ProductId == productId)
                .Select(item => new
                {
                    po.VendorId,
                    item.UnitCost,
                    po.Code,
                    po.PlacedOnUtc
                }))
            .GroupBy(x => x.VendorId)
            .Select(g => g.OrderByDescending(x => x.PlacedOnUtc).First())
            .OrderByDescending(x => x.PlacedOnUtc)
            .ToList();

        if (groupedByVendor.Count == 0)
            return [];

        // Lấy tên nhà cung cấp theo batch
        var vendorIds = groupedByVendor
            .Select(x => x.VendorId)
            .Distinct()
            .ToList();

        var vendors = vendorIds.Count > 0
            ? await _vendorOrderDataReader.GetByIdsAsync(vendorIds).ConfigureAwait(false)
            : [];

        var vendorMap = vendors.ToDictionary(v => v.Id, v => v.Name);

        return groupedByVendor
            .Select(x => new RecentPurchasePriceDto(
                VendorId: x.VendorId,
                VendorName: vendorMap.TryGetValue(x.VendorId, out var name)
                    ? name
                    : "Không rõ nhà cung cấp",
                UnitCost: x.UnitCost,
                PurchaseOrderCode: x.Code,
                PurchaseDateUtc: x.PlacedOnUtc))
            .ToList();
    }

    public async Task AddReceiptFeesAsync(Guid purchaseOrderId, decimal additionalShipping, decimal additionalTax)
    {
        if (additionalShipping < 0)
            throw new PurchaseOrderDataIsInvalidException("Error.ShippingAmountCannotBeNegative");
        if (additionalTax < 0)
            throw new PurchaseOrderDataIsInvalidException("Error.TaxAmountCannotBeNegative");

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        purchaseOrder.AccumulatedShippingAmount += additionalShipping;
        purchaseOrder.AccumulatedTaxAmount += additionalTax;
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderByCodeAsync(string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);

        var query = from po in _purchaseOrderDataReader.DataSource
                    where po.Code == code
                    select po;

        var purchaseOrder = await query.SingleOrDefaultAsync().ConfigureAwait(false);
        if (purchaseOrder is null)
            return null;

        return purchaseOrder.ToDto();
    }

    public async Task AcceptOversupplyToMainWarehouseAsync(
        Guid purchaseOrderId, Guid purchaseOrderItemId, decimal oversupplyQuantity, Guid warehouseId,
        CancellationToken ct = default)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false)
            ?? throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        var item = purchaseOrder.Items.FirstOrDefault(i => i.Id == purchaseOrderItemId)
            ?? throw new PurchaseOrderItemIsNotFoundException();

        purchaseOrder.MarkOversupplyAccepted(item.Id, warehouseId, oversupplyQuantity, item.UnitCost);
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }

    public async Task CancelAsync(Guid purchaseOrderId)
    {
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        purchaseOrder.Cancel();
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }

    #region Helper Classes

    private sealed class CreatedPurchaseOrderItemBucket
    {
        public required PurchaseOrderItem PurchaseOrderItem { get; init; }
        public required Guid ProductId { get; init; }
        public required decimal UnitCost { get; init; }
        public decimal RemainingQuantity { get; set; }
    }

    private sealed record GoodsReceiptBucket(Guid ProductId, decimal? UnitCost, IList<GoodsReceiptReceivedItem> ReceivedItems)
    {
        public decimal TotalReceivedQuantity { get; set; }
    }

    private sealed record GoodsReceiptReceivedItem(Guid ItemId)
    {
        public decimal ReceivedQuantity { get; set; }
    }

    private sealed record PurchaseOrderBucket(Guid ProductId, decimal UnitCost, IList<PurchaseOrderItemRemainder> Participants)
    {
        public decimal TotalRemainingQuantity { get; set; }
    }

    private sealed record PurchaseOrderItemRemainder(Guid ItemId)
    {
        public decimal RemainingQuantity { get; set; }
    }

    #endregion

    #region Helper Method

    private Task<string> GenerateCodeAsync()
    {
        var prefix = $"{PurchaseOrder.CODE_PREFIX}-{DateTime.UtcNow:yyMM}";
        return _codeGenerator.NextAsync(prefix, () => _purchaseOrderDataReader.SecuredDataSource.CountAsync(d => d.Code.StartsWith(prefix)));
    }

    private async Task<PurchaseOrder> CreatePurchaseOrderAggregateAsync(
        Guid vendorId, Guid? warehouseId, DateTime placedOnUtc,
        DateTime? expectedDeliveryDateUtc, string? note, decimal taxAmount, decimal shippingAmount)
    {
        var vendor = await _vendorOrderDataReader.GetByIdAsync(vendorId).ConfigureAwait(false);
        if (vendor is null)
            throw new VendorIsNotFoundException(vendorId);

        if (warehouseId.HasValue)
        {
            var warehouse = await _warehouseOrderDataReader.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
                throw new WarehouseIsNotFoundException(warehouseId.Value);
        }

        var code = await GenerateCodeAsync().ConfigureAwait(false);
        var purchaseOrder = await PurchaseOrder.CreateBuilder()
            .WithCode(code, this)
            .WithVendor(vendorId, _vendorOrderDataReader)
            .WithWarehouse(warehouseId, _warehouseOrderDataReader)
            .BuildAsync(_purchaseOrderRepository, _currentUserAccessor);
        purchaseOrder.Note = note;
        purchaseOrder.ExpectedDeliveryDateUtc = expectedDeliveryDateUtc;
        purchaseOrder.SetPlacedDate(placedOnUtc);
        purchaseOrder.TaxAmount = taxAmount;
        purchaseOrder.ShippingAmount = shippingAmount;

        return purchaseOrder;
    }

    private async Task<List<(PurchaseOrderItem PurchaseOrderItem, CreatePoFromShortageItemDto Source, bool IsNew)>> AddOrMergeShortageItemsAsync(
        PurchaseOrder purchaseOrder, IList<CreatePoFromShortageItemDto> items)
    {
        var allocationSources = new List<(PurchaseOrderItem PurchaseOrderItem, CreatePoFromShortageItemDto Source, bool IsNew)>();
        foreach (var item in items)
        {
            item.Verify();

            var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(poItem =>
                poItem.ProductId == item.ProductId && poItem.UnitCost == item.UnitCost);
            var isNew = purchaseOrderItem is null;
            if (purchaseOrderItem is null)
            {
                purchaseOrderItem = new PurchaseOrderItem(purchaseOrder.Id, item.ProductId, item.Quantity, item.UnitCost)
                {
                    Note = item.Note
                };
                await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, _productDataReader, requireVendorProduct: false).ConfigureAwait(false);
            }
            else
            {
                purchaseOrderItem.QuantityOrdered += item.Quantity;
            }

            allocationSources.Add((purchaseOrderItem, item, isNew));
        }

        return allocationSources;
    }

    private async Task AllocateShortageItemsAsync(IEnumerable<(PurchaseOrderItem PurchaseOrderItem, CreatePoFromShortageItemDto Source, bool IsNew)> allocationSources)
    {
        foreach (var (purchaseOrderItem, source, _) in allocationSources)
        {
            var allocationQuantity = source.AllocationQuantity ?? source.Quantity;
            if (allocationQuantity <= 0)
                continue;

            await _purchaseOrderAllocationManager
                .AllocatePurchaseOrderItemForOrder(new AllocatePurchaseOrderItemForOrder
                {
                    PurchaseOrderItemId = (purchaseOrderItem.PurchaseOrderId, purchaseOrderItem.Id),
                    OrderItemId = source.OrderItemId,
                    AllocationQuantity = allocationQuantity,
                    DirectShipInfo = source.DirectShipInfo is not null
                        ? new AllocatePurchaseOrderItemForOrder.AllocateDirectShipInfo(source.DirectShipInfo.ContactName, source.DirectShipInfo.ContactPhone, source.DirectShipInfo.Address)
                        {
                            Priority = source.DirectShipInfo.Priority
                        } : null
                })
                .ConfigureAwait(false);
        }
    }

    private static List<(PurchaseOrderItem PurchaseOrderItem, CreatePoFromShortageItemDto Source, bool IsNew)>
        MapCreatedPurchaseOrderItemsForShortage(PurchaseOrder purchaseOrder, IList<CreatePoFromShortageItemDto> items)
    {
        var buckets = purchaseOrder.Items
            .Select(item => new CreatedPurchaseOrderItemBucket
            {
                PurchaseOrderItem = item,
                ProductId = item.ProductId,
                UnitCost = item.UnitCost,
                RemainingQuantity = item.QuantityOrdered
            })
            .ToList();
        var allocationSources = new List<(PurchaseOrderItem PurchaseOrderItem, CreatePoFromShortageItemDto Source, bool IsNew)>();

        foreach (var item in items)
        {
            var remainingAllocationQuantity = item.AllocationQuantity ?? item.Quantity;
            if (remainingAllocationQuantity <= 0)
                continue;

            foreach (var bucket in buckets.Where(bucket =>
                         bucket.ProductId == item.ProductId
                         && bucket.UnitCost == item.UnitCost
                         && bucket.RemainingQuantity > 0))
            {
                var allocationQuantity = Math.Min(remainingAllocationQuantity, bucket.RemainingQuantity);
                allocationSources.Add((bucket.PurchaseOrderItem, item with { AllocationQuantity = allocationQuantity }, true));

                bucket.RemainingQuantity -= allocationQuantity;
                remainingAllocationQuantity -= allocationQuantity;
                if (remainingAllocationQuantity <= 0)
                    break;
            }

            if (remainingAllocationQuantity > 0)
                throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemIsNotFound");
        }

        return allocationSources;
    }

    private static List<GoodsReceiptBucket> BuildGoodsReceiptBuckets(GoodsReceipt goodsReceipt)
        => goodsReceipt.Items
            .GroupBy(
                item => (item.ProductId, item.UnitCost),
                item => (receivingQuantity: item.Quantity, item.Id),
                (key, infos) => new GoodsReceiptBucket(
                    key.ProductId, key.UnitCost,
                    infos.Select(info => new GoodsReceiptReceivedItem(info.Id) { ReceivedQuantity = info.receivingQuantity }).ToList())
                {
                    TotalReceivedQuantity = infos.Sum(info => info.receivingQuantity)
                })
            .ToList();

    private static List<PurchaseOrderBucket> BuildPurchaseOrderBuckets(PurchaseOrder purchaseOrder)
        => purchaseOrder.Items
            .Where(item => item.QuantityOrdered > item.QuantityReceived)
            .GroupBy(
                item => (item.ProductId, item.UnitCost),
                item => (remainQuantity: item.QuantityOrdered - item.QuantityReceived, item.Id),
                (key, infos) => new PurchaseOrderBucket(key.ProductId, key.UnitCost,
                    infos.Select(info => new PurchaseOrderItemRemainder(info.Id) { RemainingQuantity = info.remainQuantity }).ToList())
                {
                    TotalRemainingQuantity = infos.Sum(info => info.remainQuantity)
                })
            .ToList();

    private static void ResolveCostedItems(
        List<GoodsReceiptBucket> grBuckets,
        List<PurchaseOrderBucket> poBuckets,
        List<(Guid itemId, decimal qty)> resolvePoItems)
    {
        foreach (var grBucket in grBuckets.Where(gr => gr.UnitCost.HasValue))
        {
            var poBucket = poBuckets.FirstOrDefault(po =>
                po.ProductId == grBucket.ProductId && po.UnitCost == grBucket.UnitCost);

            if (poBucket is null || poBucket.TotalRemainingQuantity < grBucket.TotalReceivedQuantity)
                throw new GoodsReceiptItemCannotResolvedWhenSetToPurchaseOrderException(grBucket.ProductId, grBucket.TotalReceivedQuantity);

            foreach (var participant in poBucket.Participants.Where(p => p.RemainingQuantity > 0))
            {
                if (grBucket.TotalReceivedQuantity <= 0) break;

                var resolvedQty = Math.Min(participant.RemainingQuantity, grBucket.TotalReceivedQuantity);

                participant.RemainingQuantity -= resolvedQty;
                poBucket.TotalRemainingQuantity -= resolvedQty;
                grBucket.TotalReceivedQuantity -= resolvedQty;

                resolvePoItems.Add((participant.ItemId, resolvedQty));
            }
        }
    }

    private static void ResolveUncostedItems(
        List<GoodsReceiptBucket> grBuckets,
        List<PurchaseOrderBucket> poBuckets,
        List<(Guid itemId, decimal qty)> resolvePoItems,
        List<(Guid grItemId, decimal qty, decimal unitCost)> needUpdateUnitCostItems)
    {
        foreach (var grBucket in grBuckets.Where(gr => !gr.UnitCost.HasValue))
        {
            foreach (var receivedItem in grBucket.ReceivedItems.Where(ri => ri.ReceivedQuantity > 0))
            {
                var compatiblePos = poBuckets.Where(po => po.ProductId == grBucket.ProductId && po.TotalRemainingQuantity > 0);

                foreach (var poBucket in compatiblePos)
                {
                    if (receivedItem.ReceivedQuantity <= 0) break;

                    foreach (var participant in poBucket.Participants.Where(p => p.RemainingQuantity > 0))
                    {
                        if (receivedItem.ReceivedQuantity <= 0) break;

                        var resolvedQty = Math.Min(participant.RemainingQuantity, receivedItem.ReceivedQuantity);

                        receivedItem.ReceivedQuantity -= resolvedQty;
                        participant.RemainingQuantity -= resolvedQty;
                        poBucket.TotalRemainingQuantity -= resolvedQty;
                        grBucket.TotalReceivedQuantity -= resolvedQty;

                        needUpdateUnitCostItems.Add((receivedItem.ItemId, resolvedQty, poBucket.UnitCost));
                        resolvePoItems.Add((participant.ItemId, resolvedQty));
                    }
                }
            }

            if (grBucket.TotalReceivedQuantity > 0)
                throw new GoodsReceiptItemCannotResolvedWhenSetToPurchaseOrderException(grBucket.ProductId, grBucket.TotalReceivedQuantity);
        }
    }

    private static void ApplyCostAssignmentsAndSplit(
        GoodsReceipt goodsReceipt,
        Guid purchaseOrderId,
        List<(Guid grItemId, decimal qty, decimal unitCost)> assignments)
    {
        foreach (var group in assignments.GroupBy(i => i.grItemId))
        {
            var assignmentList = group.ToList();
            var goodsReceiptItem = goodsReceipt.Items.First(item => item.Id == group.Key);

            for (var i = 0; i < assignmentList.Count; i++)
            {
                var (originalItemId, qty, unitCost) = assignmentList[i];
                var isLast = i == assignmentList.Count - 1;
                if (isLast)
                {
                    goodsReceiptItem.SetUnitCost(unitCost);
                    goodsReceipt.MarkItemUnitCostSet(goodsReceiptItem.Id);
                }
                else
                {
                    goodsReceipt.SplitToNewItemWithQuantity(originalItemId, qty);
                    var newItem = goodsReceipt.Items.Last();
                    newItem.SetUnitCost(unitCost);
                    goodsReceipt.MarkItemUnitCostSet(newItem.Id);
                    goodsReceipt.RaiseItemSplitOnLinking(
                        purchaseOrderId, originalItemId, newItem.ProductId, qty, unitCost);
                }
            }
        }
    }

    #endregion
}

using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Domain.Services.PurchaseOrders;

public sealed class PurchaseOrderManager : IPurchaseOrderManager
{
    private readonly IRepository<PurchaseOrder> _purchaseOrderRepository;
    private readonly IEntityDataReader<PurchaseOrder> _purchaseOrderDataReader;
    private readonly IEntityDataReader<Vendor> _vendorOrderDataReader;
    private readonly IEntityDataReader<Warehouse> _warehouseOrderDataReader;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IGoodsReceiptManager _goodsReceiptManager;
    private readonly IPurchaseOrderAllocationManager _purchaseOrderAllocationManager;
    private readonly IDirectShipManager _directShipManager;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public PurchaseOrderManager(IRepository<PurchaseOrder> poRepository, IEntityDataReader<PurchaseOrder> purchaseOrderDataReader,
        IEntityDataReader<Vendor> vendorOrderDataReader, IEntityDataReader<Warehouse> warehouseOrderDataReader,
        IEntityDataReader<Product> productDataReader,
        IGoodsReceiptManager goodsReceiptManager, IPurchaseOrderAllocationManager purchaseOrderAllocationManager,
        IDirectShipManager directShipManager,
        ICurrentUserAccessor currentUserAccessor)
    {
        _purchaseOrderRepository = poRepository;
        _purchaseOrderDataReader = purchaseOrderDataReader;
        _vendorOrderDataReader = vendorOrderDataReader;
        _warehouseOrderDataReader = warehouseOrderDataReader;
        _productDataReader = productDataReader;
        _goodsReceiptManager = goodsReceiptManager;
        _purchaseOrderAllocationManager = purchaseOrderAllocationManager;
        _directShipManager = directShipManager;
        _currentUserAccessor = currentUserAccessor;
    }

    private Task<string> GenerateCodeAsync()
    {
        var monthPrefix = $"{PurchaseOrder.CODE_PREFIX}-{DateTime.UtcNow:yyMM}";
        var count = ((EntityDataReader<PurchaseOrder>)_purchaseOrderDataReader).SecuredDataSource.Count(d => d.Code.StartsWith(monthPrefix));
        return Task.FromResult($"{monthPrefix}-{(count + 1):D3}");
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
            .BuildAsync(_purchaseOrderDataReader, _currentUserAccessor);
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

            var allocation = await _purchaseOrderAllocationManager.AllocateAsync(purchaseOrderItem.Id, source.OrderItemId, allocationQuantity).ConfigureAwait(false);
            if (source.DirectShipInfo is { } ds)
            {
                await _directShipManager.MarkAllocationAsDirectShipAsync(
                    allocation.Id, ds.Address, ds.ContactName, ds.ContactPhone, ds.Priority).ConfigureAwait(false);
            }
        }
    }

    private sealed class CreatedPurchaseOrderItemBucket
    {
        public required PurchaseOrderItem PurchaseOrderItem { get; init; }
        public required Guid ProductId { get; init; }
        public required decimal UnitCost { get; init; }
        public decimal RemainingQuantity { get; set; }
    }

    private static List<(PurchaseOrderItem PurchaseOrderItem, CreatePoFromShortageItemDto Source, bool IsNew)> MapCreatedPurchaseOrderItemsForShortage(
        PurchaseOrder purchaseOrder,
        IList<CreatePoFromShortageItemDto> items)
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

    public Task<ExistingDraftPurchaseOrderDto?> FindDraftForVendorAsync(Guid vendorId)
    {
        if (vendorId == Guid.Empty)
            throw new VendorIsNotFoundException(vendorId);

        var draft = _purchaseOrderDataReader.DataSource
            .Where(purchaseOrder => purchaseOrder.VendorId == vendorId && purchaseOrder.Status == PurchaseOrderStatus.Draft)
            .OrderByDescending(purchaseOrder => purchaseOrder.CreatedOnUtc)
            .FirstOrDefault();

        if (draft is null)
            return Task.FromResult<ExistingDraftPurchaseOrderDto?>(null);

        return Task.FromResult<ExistingDraftPurchaseOrderDto?>(new ExistingDraftPurchaseOrderDto(draft.Id)
        {
            VendorId = draft.VendorId,
            Code = draft.Code,
            CreatedOnUtc = draft.CreatedOnUtc,
            ExpectedDeliveryDateUtc = draft.ExpectedDeliveryDateUtc
        });
    }

    public async Task<IList<RelatedPurchaseOrderDto>> FindRelatedPurchaseOrdersAsync(
        Guid vendorId,
        IList<Guid> productIds,
        IList<PurchaseOrderStatus> statuses)
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

        var products = _productDataReader.DataSource
            .Where(product => productIdSet.Contains(product.Id))
            .ToDictionary(product => product.Id, product => product.Name);

        var purchaseOrders = _purchaseOrderDataReader.DataSource
            .Where(purchaseOrder => purchaseOrder.VendorId == vendorId && statusSet.Contains(purchaseOrder.Status))
            .OrderByDescending(purchaseOrder => purchaseOrder.CreatedOnUtc)
            .ToList();

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
            createPurchaseOrderDto.Items.Add(new PurchaseOrderItemDto(Guid.NewGuid())
            {
                PurchaseOrderId = Guid.Empty,
                ProductId = item.ProductId,
                QuantityOrdered = item.Quantity,
                UnitCost = item.UnitCost,
                Note = item.Note
            });
        }

        var createResult = await CreatePurchaseOrderAsync(createPurchaseOrderDto).ConfigureAwait(false);
        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(createResult.CreatedId).ConfigureAwait(false);
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

        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);
        if (purchaseOrder.Status != PurchaseOrderStatus.Draft)
            throw new PurchaseOrderCannotUpdateOrderItemsException();

        var allocationSources = await AddOrMergeShortageItemsAsync(purchaseOrder, items).ConfigureAwait(false);
        foreach (var allocationSource in allocationSources.Where(source => source.IsNew))
            purchaseOrder.MarkItemAdded(allocationSource.PurchaseOrderItem);

        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;
        purchaseOrder.MarkUpdated();
        var updatedPurchaseOrder = await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
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

        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
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

        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(dto.PurchaseOrderId);

        var product = await _productDataReader.GetByIdAsync(dto.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(dto.ProductId);

        if (!purchaseOrder.CanUpdatePurchaseOrderItems())
            throw new PurchaseOrderCannotAddItemException();

        var purchaseOrderItem = new PurchaseOrderItem(dto.PurchaseOrderId, dto.ProductId, dto.QuantityOrdered, dto.UnitCost)
        {
            Note = dto.Note,
        };
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, _productDataReader).ConfigureAwait(false);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        purchaseOrder.MarkItemAdded(purchaseOrderItem);
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        return new AddPurchaseOrderItemResultDto
        {
            PurchaseOrderId = purchaseOrder.Id,
            CreatedItemId = purchaseOrderItem.Id
        };
    }

    public async Task ClosePartialAsync(Guid purchaseOrderId, string reason)
    {
        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        var oldStatus = purchaseOrder.Status;
        purchaseOrder.ClosePartial(reason);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        purchaseOrder.MarkStatusChanged(oldStatus);
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }

    public async Task ChangeStatusAsync(Guid purchaseOrderId, PurchaseOrderStatus status)
    {
        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
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
        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        return purchaseOrder.CanChangeStatusTo(status);
    }

    public async Task<bool> CanAddPurchaseOrderItemsAsync(Guid purchaseOrderId)
    {
        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        return purchaseOrder.CanUpdatePurchaseOrderItems();
    }

    public Task<bool> DoesCodeExistAsync(string code, Guid? comparesWithCurrentId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);

        var query = from purchaseOrder in _purchaseOrderDataReader.DataSource
                    where purchaseOrder.Code == code && (comparesWithCurrentId == null || purchaseOrder.Id != comparesWithCurrentId)
                    select purchaseOrder;

        var sameNameExists = query.FirstOrDefault() != null;
        return Task.FromResult(sameNameExists);
    }

    public async Task<ReceivedGoodsForItemResultDto> ReceiveItemsAsync(ReceivedGoodsForItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
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
            throw new PurchaseOrderItemIsNotFoundException(dto.PurchaseOrderItemId);

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
            EnsureDirectShipTransitWarehouseConfigured();

        purchaseOrderItem.AddQuantityReceived(dto.ReceivedQuantity);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        purchaseOrder.MarkItemReceived(purchaseOrderItem.Id, dto.ReceivedQuantity);

        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        var grResult = await _goodsReceiptManager.CreateFromPurchaseOrderReceivingAsync(new CreateGoodsReceiptFromPurchaseOrderDto
        {
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

        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
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
            hasReceivableDirectShip = await _directShipManager
                .HasReceivableDirectShipAllocationsAsync(itemId)
                .ConfigureAwait(false);
            if (hasReceivableDirectShip)
                break;
        }

        if (hasReceivableDirectShip)
            EnsureDirectShipTransitWarehouseConfigured();

        // Aggregate-validate qty theo PO item (cùng item nhiều kho phải cộng dồn trước khi check).
        var groupedByItem = dto.Lines.GroupBy(line => line.PurchaseOrderItemId);
        foreach (var group in groupedByItem)
        {
            var poItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == group.Key);
            if (poItem is null)
                throw new PurchaseOrderItemIsNotFoundException(group.Key);

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
            var result = await ReceiveSingleItemAsync(
                purchaseOrder,
                new ReceivedGoodsForItemDto(purchaseOrder.Id, line.PurchaseOrderItemId)
                {
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

    private void EnsureDirectShipTransitWarehouseConfigured()
    {
        var transitWarehouse = _warehouseOrderDataReader.DataSource
            .FirstOrDefault(w => w.WarehouseType == WarehouseType.DirectShipTransit);

        if (transitWarehouse is null)
            throw new DirectShipTransitWarehouseNotConfiguredException();
    }

    private async Task ProcessDirectShipReceiptsAsync(
        IReadOnlyList<AllocationReceiptDto> receipts,
        Guid sourceGoodsReceiptId,
        Guid receivedWarehouseId)
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
        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
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

            var vendorIds = _vendorOrderDataReader.DataSource
                .Where(v => v.Name.ToUpper().Contains(uppercaseKeywords) || v.Name.ToUpper().Contains(normalizedKeywords) || v.NormalizedName.Contains(normalizedKeywords))
                .Select(v => v.Id)
                .ToList()
                .OfType<Guid?>()
                .ToList();
            IList<Guid?> warehouseIds = [];
            IList<Guid?> userIds = [];

            query = query.Where(c => c.Code.Contains(keywords)
                || vendorIds.Contains(c.VendorId)
                || warehouseIds.Contains(c.WarehouseId) || c.Items.Any(item => warehouseIds.Contains(item.WarehouseId))
                || userIds.Contains(c.CreatedByUserId));
        }

        query = query.OrderByDescending(c => c.CreatedOnUtc);

        var totalCount = query.Count();
        var pagedData = query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        var data = PagedDataDto.Create(pagedData.Select(purchaseOrder => purchaseOrder.ToDto()), pageIndex, pageSize, totalCount);
        return data;
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id)
    {
        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(id);
        if (purchaseOrder is null)
            return null;

        return purchaseOrder.ToDto();
    }

    public async Task<bool> CanReceiveGoodsAsync(Guid purchaseOrderId)
    {
        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        return purchaseOrder.CanReceiveGoods();
    }

    public async Task DeleteOrderItemAsync(Guid purchaseOrderId, Guid itemId)
    {
        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        if (!purchaseOrder.CanUpdatePurchaseOrderItems())
            throw new PurchaseOrderCannotUpdateOrderItemsException();

        purchaseOrder.RemoveOrderItem(itemId);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;
        purchaseOrder.MarkItemRemoved(itemId);

        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }

    public async Task<IList<RecentPurchasePriceDto>> GetRecentPurchasePricesAsync(Guid productId)
    {
        // Lấy tất cả đơn nhập (không bị hủy) có chứa sản phẩm này
        var purchaseOrders = _purchaseOrderDataReader.DataSource
            .Where(po => po.Status != PurchaseOrderStatus.Cancelled
                      && po.Items.Any(item => item.ProductId == productId))
            .OrderByDescending(po => po.PlacedOnUtc)
            .ToList();

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

        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        purchaseOrder.AccumulatedShippingAmount += additionalShipping;
        purchaseOrder.AccumulatedTaxAmount += additionalTax;
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }

    public Task<PurchaseOrderDto?> GetPurchaseOrderByCodeAsync(string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);

        return Task.Run(() =>
        {
            var query = from po in _purchaseOrderDataReader.DataSource
                        where po.Code == code
                        select po;

            var purchaseOrder = query.SingleOrDefault();
            if (purchaseOrder is null)
                return null;

            return purchaseOrder.ToDto();
        });
    }

    public async Task AcceptOversupplyToMainWarehouseAsync(
        Guid purchaseOrderId, Guid purchaseOrderItemId, decimal oversupplyQuantity, Guid warehouseId,
        CancellationToken ct = default)
    {
        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(purchaseOrderId).ConfigureAwait(false)
            ?? throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        var item = purchaseOrder.Items.FirstOrDefault(i => i.Id == purchaseOrderItemId)
            ?? throw new PurchaseOrderItemIsNotFoundException(purchaseOrderItemId);

        purchaseOrder.MarkOversupplyAccepted(item.Id, warehouseId, oversupplyQuantity, item.UnitCost);
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }
}

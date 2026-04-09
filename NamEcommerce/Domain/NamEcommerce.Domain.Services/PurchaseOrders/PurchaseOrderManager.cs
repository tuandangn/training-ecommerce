using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Users;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Domain.Services.PurchaseOrders;

public sealed class PurchaseOrderManager : IPurchaseOrderManager
{
    private readonly IRepository<PurchaseOrder> _purchaseOrderRepository;
    private readonly IEntityDataReader<PurchaseOrder> _purchaseOrderDataReader;
    private readonly IEntityDataReader<Vendor> _vendorOrderDataReader;
    private readonly IEntityDataReader<Warehouse> _warehouseOrderDataReader;
    private readonly IEntityDataReader<User> _userDataReader;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IInventoryStockManager _stockManager;
    private readonly IEventPublisher _eventPublisher;
    private readonly IEntityDataReader<InventoryStock> _stockDataReader;
    private readonly IRepository<Product> _productRepository;

    public PurchaseOrderManager(IRepository<PurchaseOrder> poRepository, IEntityDataReader<PurchaseOrder> purchaseOrderDataReader,
        IInventoryStockManager stockManager, IEntityDataReader<Vendor> vendorOrderDataReader, IEntityDataReader<Warehouse> warehouseOrderDataReader,
        IEntityDataReader<User> userDataReader, IEntityDataReader<Product> productDataReader, IEventPublisher eventPublisher,
        IEntityDataReader<InventoryStock> stockDataReader, IRepository<Product> productRepository)
    {
        _purchaseOrderRepository = poRepository;
        _stockManager = stockManager;
        _purchaseOrderDataReader = purchaseOrderDataReader;
        _vendorOrderDataReader = vendorOrderDataReader;
        _warehouseOrderDataReader = warehouseOrderDataReader;
        _userDataReader = userDataReader;
        _productDataReader = productDataReader;
        _eventPublisher = eventPublisher;
        _stockDataReader = stockDataReader;
        _productRepository = productRepository;
    }

    public async Task<CreatePurchaseOrderResultDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        if (await DoesCodeExistAsync(dto.Code).ConfigureAwait(false))
            throw new PurchaseOrderCodeExistsException(dto.Code);

        if (dto.VendorId.HasValue)
        {
            var vendor = await _vendorOrderDataReader.GetByIdAsync(dto.VendorId.Value).ConfigureAwait(false);
            if (vendor is null)
                throw new VendorIsNotFoundException(dto.VendorId.Value);
        }

        if (dto.WarehouseId.HasValue)
        {
            var warehouse = await _warehouseOrderDataReader.GetByIdAsync(dto.WarehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
                throw new WarehouseIsNotFoundException(dto.WarehouseId.Value);
        }

        if (dto.CreatedByUserId.HasValue)
        {
            var user = await _userDataReader.GetByIdAsync(dto.CreatedByUserId.Value).ConfigureAwait(false);
            if (user is null)
                throw new UserIsNotFoundException(dto.CreatedByUserId.Value);
        }

        var po = new PurchaseOrder(dto.Code, dto.VendorId, dto.WarehouseId, dto.CreatedByUserId)
        {
            Note = dto.Note,
            ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc,
            ShippingAmount = dto.ShippingAmount,
            TaxAmount = dto.TaxAmount
        };
        var insertedPurchaseOrder = await _purchaseOrderRepository.InsertAsync(po).ConfigureAwait(false);

        await _eventPublisher.EntityCreated(insertedPurchaseOrder).ConfigureAwait(false);

        return new CreatePurchaseOrderResultDto
        {
            CreatedId = insertedPurchaseOrder.Id
        };
    }

    public async Task<UpdatePurchaseOrderResultDto> UpdatePurchaseOrderAsync(UpdatePurchaseOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(dto.Id);

        if (dto.VendorId.HasValue)
        {
            var vendor = await _vendorOrderDataReader.GetByIdAsync(dto.VendorId.Value).ConfigureAwait(false);
            if (vendor is null)
                throw new VendorIsNotFoundException(dto.VendorId.Value);
        }

        purchaseOrder.Note = dto.Note;
        purchaseOrder.ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc;
        purchaseOrder.ShippingAmount = dto.ShippingAmount;
        purchaseOrder.TaxAmount = dto.TaxAmount;
        await purchaseOrder.ChangeVendorAsync(dto.VendorId, _vendorOrderDataReader).ConfigureAwait(false);

        var updatedPurchaseOrder = await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(updatedPurchaseOrder).ConfigureAwait(false);

        return new UpdatePurchaseOrderResultDto(updatedPurchaseOrder.Id)
        {
            VendorId = updatedPurchaseOrder.VendorId,
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

        if (!purchaseOrder.CanAddPurchaseOrderItems())
            throw new PurchaseOrderCannotAddItemException();

        var purchaseOrderItem = new PurchaseOrderItem(dto.PurchaseOrderId, dto.ProductId, dto.QuantityOrdered, dto.UnitCost)
        {
            Note = dto.Note,
        };
        await purchaseOrder.AddPurchaseOrderItemAsync(purchaseOrderItem, _productDataReader).ConfigureAwait(false);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        var updatedPurchaseOrder = await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        await _eventPublisher.EntityCreated(purchaseOrderItem).ConfigureAwait(false);
        await _eventPublisher.EntityUpdated(updatedPurchaseOrder).ConfigureAwait(false);

        return new AddPurchaseOrderItemResultDto
        {
            PurchaseOrderId = purchaseOrder.Id,
            CreatedItemId = purchaseOrderItem.Id
        };
    }

    public async Task ChangeStatusAsync(Guid purchaseOrderId, PurchaseOrderStatus status)
    {
        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        if (!purchaseOrder.CanChangeStatusTo(status))
            throw new PurchaseOrderCannotChangeStatusException();

        purchaseOrder.ChangeStatus(status);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        var updatedPurchaseOrder = await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(updatedPurchaseOrder).ConfigureAwait(false);
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

        return purchaseOrder.CanAddPurchaseOrderItems();
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

        if (!purchaseOrder.CanReceiveGoods())
            throw new PurchaseOrderCannotReceiveGoodsException();

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == dto.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
            throw new PurchaseOrderItemIsNotFoundException(dto.PurchaseOrderItemId);

        if (purchaseOrderItem.QuantityReceived + dto.ReceivedQuantity > purchaseOrderItem.QuantityOrdered)
            throw new PurchaseOrderReceiveQuantityExceedsOrderedQuantityException();

        var product = await _productDataReader.GetByIdAsync(purchaseOrderItem.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(purchaseOrderItem.ProductId);

        Guid? warehouseId = purchaseOrder.WarehouseId ?? dto.WarehouseId ?? null;
        if (product.TrackInventory)
        {
            if (!warehouseId.HasValue)
                throw new ArgumentException("Warehouse is required", nameof(dto));
            else
            {
                var warehouse = await _warehouseOrderDataReader.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
                if (warehouse is null)
                    throw new WarehouseIsNotFoundException(warehouseId.Value);
            }
        }

        purchaseOrderItem.AddQuantityReceived(dto.ReceivedQuantity);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        var updatedPurchaseOrder = await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        // Calculate Weighted Average Cost Price
        var allStocks = _stockDataReader.DataSource.Where(s => s.ProductId == product.Id).ToList();
        var currentTotalStock = allStocks.Sum(s => s.QuantityOnHand);

        var currentTotalValue = product.CostPrice * currentTotalStock;
        var receivedValue = dto.ReceivedQuantity * purchaseOrderItem.UnitCost;
        var newTotalStock = currentTotalStock + dto.ReceivedQuantity;

        if (newTotalStock > 0)
        {
            var newCostPrice = (currentTotalValue + receivedValue) / newTotalStock;
            product.SetCostPrice(newCostPrice);
            product.UpdatedOnUtc = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product).ConfigureAwait(false);
        }

        if (product.TrackInventory)
        {
            await _stockManager.ReceiveStockAsync(purchaseOrderItem.ProductId,
                warehouseId!.Value, dto.ReceivedQuantity,
                $"Nhập hàng từ PO {purchaseOrder.Code}", dto.ReceivedByUserId,
                (int)StockReferenceType.PurchaseOrder, purchaseOrder.Id).ConfigureAwait(false);
        }

        await _eventPublisher.EntityUpdated(updatedPurchaseOrder).ConfigureAwait(false);

        return new ReceivedGoodsForItemResultDto(purchaseOrder.Id, purchaseOrderItem.Id)
        {
            ReceivedQuantity = dto.ReceivedQuantity
        };
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

    public async Task<IPagedDataDto<PurchaseOrderDto>> GetPurchaseOrdersAsync(string? keywords, int pageIndex, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0, nameof(pageIndex));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0, nameof(pageSize));

        var query = _purchaseOrderDataReader.DataSource;

        if (!string.IsNullOrEmpty(keywords))
        {
            var normalizedKeywords = TextHelper.Normalize(keywords);

            var vendorIds = _vendorOrderDataReader.DataSource
                .Where(v => v.Name.Contains(keywords) || v.Name.Contains(normalizedKeywords) || v.NormalizedName.Contains(keywords))
                .Select(v => v.Id)
                .ToList()
                .OfType<Guid?>()
                .ToList();
            var warehouseIds = _warehouseOrderDataReader.DataSource
                .Where(w => w.Name.Contains(keywords) || w.Name.Contains(normalizedKeywords) || w.NormalizedName.Contains(keywords))
                .Select(w => w.Id)
                .ToList()
                .OfType<Guid?>()
                .ToList();
            IList<Guid?> userIds = [];

            query = query.Where(c => c.Code.Contains(keywords)
                || vendorIds.Contains(c.VendorId)
                || warehouseIds.Contains(c.WarehouseId)
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
}

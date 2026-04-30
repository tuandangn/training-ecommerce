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
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Users;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.Users;

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
    private readonly IEntityDataReader<InventoryStock> _stockDataReader;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<ProductPriceHistory> _priceHistoryRepository;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public PurchaseOrderManager(IRepository<PurchaseOrder> poRepository, IEntityDataReader<PurchaseOrder> purchaseOrderDataReader,
        IInventoryStockManager stockManager, IEntityDataReader<Vendor> vendorOrderDataReader, IEntityDataReader<Warehouse> warehouseOrderDataReader,
        IEntityDataReader<User> userDataReader, IEntityDataReader<Product> productDataReader,
        IEntityDataReader<InventoryStock> stockDataReader, IRepository<Product> productRepository,
        IRepository<ProductPriceHistory> priceHistoryRepository, ICurrentUserAccessor currentUserAccessor)
    {
        _purchaseOrderRepository = poRepository;
        _stockManager = stockManager;
        _purchaseOrderDataReader = purchaseOrderDataReader;
        _vendorOrderDataReader = vendorOrderDataReader;
        _warehouseOrderDataReader = warehouseOrderDataReader;
        _userDataReader = userDataReader;
        _productDataReader = productDataReader;
        _stockDataReader = stockDataReader;
        _productRepository = productRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<CreatePurchaseOrderResultDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        if (await DoesCodeExistAsync(dto.Code).ConfigureAwait(false))
            throw new PurchaseOrderCodeExistsException(dto.Code);

        var vendor = await _vendorOrderDataReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor is null)
            throw new VendorIsNotFoundException(dto.VendorId);

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

        var purchaseOrder = await PurchaseOrder.CreateBuilder()
            .WithCode(dto.Code, this)
            .WithVendor(dto.VendorId, _vendorOrderDataReader)
            .WithWarehouse(dto.WarehouseId, _warehouseOrderDataReader)
            .BuildAsync(_purchaseOrderDataReader, _currentUserAccessor);
        purchaseOrder.Note = dto.Note;
        purchaseOrder.ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc;
        purchaseOrder.SetPlacedDate(dto.PlacedOnUtc);
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

    public async Task ChangeStatusAsync(Guid purchaseOrderId, PurchaseOrderStatus status)
    {
        var purchaseOrder = await _purchaseOrderDataReader.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        if (!purchaseOrder.CanChangeStatusTo(status))
            throw new PurchaseOrderCannotChangeStatusException();

        var oldStatus = purchaseOrder.Status;
        purchaseOrder.ChangeStatus(status);
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

        if (!purchaseOrder.CanReceiveGoods())
            throw new PurchaseOrderCannotReceiveGoodsException();

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == dto.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
            throw new PurchaseOrderItemIsNotFoundException(dto.PurchaseOrderItemId);

        var product = await _productDataReader.GetByIdAsync(purchaseOrderItem.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(purchaseOrderItem.ProductId);

        Guid? warehouseId = purchaseOrder.WarehouseId ?? dto.WarehouseId ?? null;
        if (!warehouseId.HasValue)
            throw new ArgumentException("Warehouse is required", nameof(dto));
        else
        {
            var warehouse = await _warehouseOrderDataReader.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
                throw new WarehouseIsNotFoundException(warehouseId.Value);
        }

        purchaseOrderItem.AddQuantityReceived(dto.ReceivedQuantity);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;
        purchaseOrder.MarkItemReceived(purchaseOrderItem.Id, dto.ReceivedQuantity);

        var updatedPurchaseOrder = await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        // Calculate Weighted Average Cost Price
        var allStocks = _stockDataReader.DataSource.Where(s => s.ProductId == product.Id).ToList();
        var currentTotalStock = allStocks.Sum(s => s.QuantityOnHand);

        var currentTotalValue = product.CostPrice * currentTotalStock;
        var receivedValue = dto.ReceivedQuantity * purchaseOrderItem.UnitCost;
        var newTotalStock = currentTotalStock + dto.ReceivedQuantity;

        var oldUnitPrice = product.UnitPrice;
        var oldCostPrice = product.CostPrice;

        var newCostPrice = newTotalStock > 0
            ? (currentTotalValue + receivedValue) / newTotalStock
            : oldCostPrice;

        // Nếu người dùng không nhập giá bán mới thì giữ nguyên giá bán hiện tại
        var newUnitPrice = dto.SellingPrice ?? oldUnitPrice;

        if (newUnitPrice != oldUnitPrice || newCostPrice != oldCostPrice)
        {
            product.UpdatePrice(newUnitPrice, newCostPrice);
            product.UpdatedOnUtc = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product).ConfigureAwait(false);

            await _priceHistoryRepository.InsertAsync(new ProductPriceHistory(
                product.Id, oldUnitPrice, newUnitPrice, oldCostPrice, newCostPrice,
                $"Nhập hàng từ PO {purchaseOrder.Code}")
            ).ConfigureAwait(false);
        }

        await _stockManager.ReceiveStockAsync(purchaseOrderItem.ProductId,
            warehouseId.Value, dto.ReceivedQuantity,
            $"Nhập hàng từ PO {purchaseOrder.Code}", dto.ReceivedByUserId,
            (int)StockReferenceType.PurchaseOrder, purchaseOrder.Id).ConfigureAwait(false);

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
}

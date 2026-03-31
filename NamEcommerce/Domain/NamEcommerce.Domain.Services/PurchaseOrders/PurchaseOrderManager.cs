using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Users;
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

    public PurchaseOrderManager(IRepository<PurchaseOrder> poRepository, IEntityDataReader<PurchaseOrder> purchaseOrderDataReader,
        IInventoryStockManager stockManager, IEntityDataReader<Vendor> vendorOrderDataReader, IEntityDataReader<Warehouse> warehouseOrderDataReader,
        IEntityDataReader<User> userDataReader, IEntityDataReader<Product> productDataReader, IEventPublisher eventPublisher)
    {
        _purchaseOrderRepository = poRepository;
        _stockManager = stockManager;
        _purchaseOrderDataReader = purchaseOrderDataReader;
        _vendorOrderDataReader = vendorOrderDataReader;
        _warehouseOrderDataReader = warehouseOrderDataReader;
        _userDataReader = userDataReader;
        _productDataReader = productDataReader;
        _eventPublisher = eventPublisher;
    }

    public async Task<CreatePurchaseOrderResultDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

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

        var user = await _userDataReader.GetByIdAsync(dto.CreatedByUserId).ConfigureAwait(false);
        if (user is null)
            throw new UserIsNotFoundException(dto.CreatedByUserId);

        var po = new PurchaseOrder(dto.Code, dto.VendorId, dto.WarehouseId, dto.CreatedByUserId)
        {
            Note = dto.Note,
            ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDate,
            ShippingAmount = dto.ShippingAmount,
            TaxAmount = dto.TaxAmount,
        };
        po.ChangeStatus(dto.Status);
        var insertedPurchaseOrder = await _purchaseOrderRepository.InsertAsync(po).ConfigureAwait(false);

        await _eventPublisher.EntityCreated(insertedPurchaseOrder).ConfigureAwait(false);

        return new CreatePurchaseOrderResultDto
        {
            CreatedId = insertedPurchaseOrder.Id
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

        if (!purchaseOrder.CanAddPurchaseOrderItem())
            throw new PurchaseOrderCannotAddItemException();

        var purchaseOrderItem = new PurchaseOrderItem(dto.PurchaseOrderId, dto.ProductId, dto.QuantityOrdered, dto.UnitCost)
        {
            Note = dto.Note,
        };
        purchaseOrder.AddPurchaseOrderItem(purchaseOrderItem);
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
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        if (!purchaseOrder.CanChangeStatusTo(status))
            throw new PurchaseOrderCannotChangeStatusException();

        purchaseOrder.ChangeStatus(status);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        var updatedPurchaseOrder = await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(updatedPurchaseOrder).ConfigureAwait(false);
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

        if (dto.ReceivedQuantity == 0)
        {
            return new ReceivedGoodsForItemResultDto(dto.PurchaseOrderId, dto.PurchaseOrderItemId)
            {
                ReceivedQuantity = 0
            };
        }

        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(dto.PurchaseOrderId);

        if (!purchaseOrder.CanReceiveGoods())
            throw new PurchaseOrderCannotReceiveGoodsException();

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == dto.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
            throw new PurchaseOrderItemIsNotFoundException(dto.PurchaseOrderItemId);

        if(purchaseOrderItem.QuantityReceived + dto.ReceivedQuantity > purchaseOrderItem.QuantityOrdered)
            throw new PurchaseOrderReceiveQuantityExceedsOrderedQuantityException();

        var product = await _productDataReader.GetByIdAsync(purchaseOrderItem.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(purchaseOrderItem.ProductId);

        purchaseOrderItem.AddQuantityReceived(dto.ReceivedQuantity);
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;

        var updatedPurchaseOrder = await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);

        if (purchaseOrder.WarehouseId.HasValue)
        {
            await _stockManager.ReceiveStockAsync(purchaseOrderItem.ProductId,
                purchaseOrder.WarehouseId.Value, dto.ReceivedQuantity,
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
        var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(purchaseOrderId);

        var hasChanged = purchaseOrder.VerifyStatus();

        if (!hasChanged)
            return;

        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;
        await _purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }
}

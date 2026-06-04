using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Domain.Entities.PurchaseOrders;

[Serializable]
public sealed record PurchaseOrder : AppAggregateEntity
{
    public const string CODE_PREFIX = "DN";

    private PurchaseOrder() : base(Guid.Empty)
    {
        Code = string.Empty;
    }

    private PurchaseOrder(Guid id, string code, Guid vendorId, Guid? warehouseId, CurrentUserInfoDto? createdByUser) : base(id)
    {
        Code = code;
        VendorId = vendorId;
        WarehouseId = warehouseId;
        CreatedByUserId = createdByUser?.Id;

        Status = PurchaseOrderStatus.Draft;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public DateTime PlacedOnUtc { get; private set; }
    public string Code { get; private set; }
    public Guid VendorId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public Guid? CreatedByUserId { get; }
    public DateTime? ExpectedDeliveryDateUtc { get; internal set; }
    public string? Note { get; internal set; }
    public PurchaseOrderStatus Status { get; private set; }

    public string? CloseReason { get; private set; }
    public DateTime? ClosedOnUtc { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedOnUtc { get; private set; }
    public DateTime? LastReceivedOnUtc { get; private set; }

    public decimal TaxAmount { get; internal set; }
    public decimal ShippingAmount { get; internal set; }
    public decimal AccumulatedShippingAmount { get; internal set; }
    public decimal AccumulatedTaxAmount { get; internal set; }
    public decimal TotalShipping => ShippingAmount + AccumulatedShippingAmount;
    public decimal TotalTax => TaxAmount + AccumulatedTaxAmount;
    public decimal Subtotal => _items.Sum(x => x.TotalCost);
    public decimal TotalAmount => Subtotal + TotalShipping + TotalTax;

    private readonly List<PurchaseOrderItem> _items = [];
    public IEnumerable<PurchaseOrderItem> Items => _items.AsReadOnly();

    public DateTime CreatedOnUtc { get; }
    public DateTime? UpdatedOnUtc { get; internal set; }

    public byte[] RowVersion { get; private set; } = [];

    #region Methods

    internal void ClosePartial(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new PurchaseOrderDataIsInvalidException("Error.PurchaseOrder.CloseReasonRequired");
        if (Status != PurchaseOrderStatus.Receiving)
            throw new PurchaseOrderCannotChangeStatusException();

        Status = PurchaseOrderStatus.Completed;
        CloseReason = reason;
        ClosedOnUtc = DateTime.UtcNow;
    }
    internal void Cancel()
    {
        if (!CanChangeStatusTo(PurchaseOrderStatus.Cancelled))
            throw new PurchaseOrderCannotChangeStatusException();

        Status = PurchaseOrderStatus.Cancelled;

        MarkCancelled();
    }

    internal void SetPlacedDate(DateTime placedOnUtc)
    {
        if (placedOnUtc > DateTime.UtcNow)
            throw new PurchaseOrderDataIsInvalidException("Error.PurchaseOrder.PlacedDateGreaterThanNow");
        PlacedOnUtc = placedOnUtc;
    }

    internal async Task ChangeVendorAsync(Guid vendorId, IGetByIdService<Vendor> byIdGetter)
    {
        if (VendorId == vendorId)
            return;

        if (_items.Count is 0)
            throw new InvalidOperationException("Cannot change vendor when there are items in the purchase order.");

        ArgumentNullException.ThrowIfNull(byIdGetter);

        var vendor = await byIdGetter.GetByIdAsync(vendorId).ConfigureAwait(false);
        if (vendor is null)
            throw new VendorIsNotFoundException(vendorId);

        VendorId = vendorId;
    }

    internal async Task ChangeWarehouse(Guid? warehouseId, IGetByIdService<Warehouse> byIdGetter)
    {
        if (WarehouseId == warehouseId)
            return;

        if (!warehouseId.HasValue)
        {
            RemoveWarehouse();
            return;
        }

        ArgumentNullException.ThrowIfNull(byIdGetter);

        var warehouse = await byIdGetter.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
        if (warehouse is null)
            throw new WarehouseIsNotFoundException(warehouseId.Value);

        WarehouseId = warehouseId;
    }
    internal void RemoveWarehouse() => WarehouseId = null;

    internal async Task AddPurchaseOrderItemAsync(PurchaseOrderItem item, IGetByIdService<Product> byIdGetter, bool requireVendorProduct = true)
    {
        if (item.PurchaseOrderId != Id)
            throw new InvalidOperationException("The item does not belong to this purchase order.");

        ArgumentNullException.ThrowIfNull(byIdGetter);

        var product = await byIdGetter.GetByIdAsync(item.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(item.ProductId);

        if (requireVendorProduct && !product.ProductVendors.Any(v => v.VendorId == VendorId))
            throw new InvalidOperationException("The product does not belong to the selected vendor.");

        _items.Add(item);
    }
    internal void RemoveOrderItem(Guid itemId)
    {
        if (!CanUpdateItems())
            throw new PurchaseOrderCannotUpdateOrderItemsException();

        var orderItem = _items.FirstOrDefault(i => i.Id == itemId);
        if (orderItem is null)
            return;

        _items.Remove(orderItem);
    }

    internal void UpdateOrderItem(Guid itemId, decimal quantityOrdered, decimal unitCost, string? note)
    {
        if (!CanUpdateItems())
            throw new PurchaseOrderCannotUpdateOrderItemsException();
        if (quantityOrdered <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemQuantityMustBePositive");
        if (unitCost < 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemUnitCostCannotBeNegative");

        var orderItem = _items.FirstOrDefault(i => i.Id == itemId);
        if (orderItem is null)
            throw new PurchaseOrderItemIsNotFoundException();

        orderItem.QuantityOrdered = quantityOrdered;
        orderItem.UnitCost = unitCost;
        orderItem.Note = note;
    }

    public bool CanUpdateItems()
        => Status == PurchaseOrderStatus.Draft
           || Status == PurchaseOrderStatus.Submitted
           || (Status == PurchaseOrderStatus.Approved && !_items.Any(item => item.QuantityReceived > 0));
    public bool CanReceiveGoods() => Status == PurchaseOrderStatus.Approved || Status == PurchaseOrderStatus.Receiving;
    private bool CanUpdateStatus() => Status != PurchaseOrderStatus.Completed && Status != PurchaseOrderStatus.Cancelled;
    public bool CanChangeStatusTo(PurchaseOrderStatus toStatus)
    {
        if (Status == PurchaseOrderStatus.Draft && toStatus == PurchaseOrderStatus.Submitted && !Items.Any())
            return false;

        if (!CanUpdateStatus())
            return false;

        if (toStatus == PurchaseOrderStatus.Cancelled && Items.Any(item => item.QuantityReceived > 0))
            return false;

        var subtract = (int)toStatus - (int)Status;
        if (subtract < 0)
            return false;

        return Enum.IsDefined(toStatus);
    }
    internal void ChangeStatus(PurchaseOrderStatus status, Guid? actingUserId = null)
    {
        if (!CanChangeStatusTo(status))
            throw new PurchaseOrderCannotChangeStatusException();

        Status = status;

        if (status == PurchaseOrderStatus.Approved && !ApprovedOnUtc.HasValue)
        {
            ApprovedByUserId = actingUserId;
            ApprovedOnUtc = DateTime.UtcNow;
        }
    }
    internal bool VerifyStatus()
    {
        if (!CanUpdateStatus())
            return false;

        if (Status != PurchaseOrderStatus.Completed && Items.Any() && Items.All(item => item.QuantityReceived >= item.QuantityOrdered))
        {
            ChangeStatus(PurchaseOrderStatus.Completed);
            return true;
        }

        if (Status != PurchaseOrderStatus.Receiving && Items.Any(item => item.QuantityReceived > 0))
        {
            ChangeStatus(PurchaseOrderStatus.Receiving);
            return true;
        }

        return false;
    }

    internal bool RevertReceivingIfEmpty()
    {
        if (Status != PurchaseOrderStatus.Receiving) return false;
        if (Items.Any(item => item.QuantityReceived > 0)) return false;

        Status = PurchaseOrderStatus.Approved;
        return true;
    }

    #endregion

    #region Domain Event Markers

    internal void MarkCreated() => RaiseDomainEvent(new PurchaseOrderCreated(Id, Code, VendorId, WarehouseId));

    internal void MarkUpdated()
        => RaiseDomainEvent(new PurchaseOrderUpdated(Id));

    internal void MarkCancelled() => RaiseDomainEvent(new PurchaseOrderCancelled(Id));

    internal void MarkStatusChanged(PurchaseOrderStatus oldStatus)
        => RaiseDomainEvent(new PurchaseOrderStatusChanged(Id, oldStatus, Status));

    internal void MarkItemAdded(PurchaseOrderItem item, string? productName = null)
        => RaiseDomainEvent(new PurchaseOrderItemAdded(Id, item.Id, item.ProductId, item.QuantityOrdered, item.UnitCost, productName, item.Note));

    internal void MarkItemUpdated(
        PurchaseOrderItem item,
        decimal oldQuantityOrdered,
        decimal oldUnitCost,
        string? oldNote,
        string? productName = null)
        => RaiseDomainEvent(new PurchaseOrderItemUpdated(
            Id,
            item.Id,
            item.ProductId,
            oldQuantityOrdered,
            item.QuantityOrdered,
            oldUnitCost,
            item.UnitCost,
            oldNote,
            item.Note,
            productName));

    internal void MarkItemRemoved(PurchaseOrderItem item, string? productName = null)
        => RaiseDomainEvent(new PurchaseOrderItemRemoved(Id, item.Id, item.ProductId, item.QuantityOrdered, item.UnitCost, productName, item.Note));

    internal void MarkItemReceived(Guid itemId, decimal receivedQuantity, Guid? goodsReceiptId = null)
    {
        LastReceivedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new PurchaseOrderItemReceived(Id, itemId, receivedQuantity, goodsReceiptId));
    }

    internal void MarkBulkReceived()
    {
        LastReceivedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new PurchaseOrderBulkReceived(Id));
    }

    internal void MarkOversupplyAccepted(Guid purchaseOrderItemId, Guid warehouseId, decimal oversupplyQuantity, decimal unitCost)
        => RaiseDomainEvent(new VendorOversupplyAccepted(purchaseOrderItemId, warehouseId, oversupplyQuantity, unitCost));

    #endregion

    #region Builder

    internal static PurchaseOrderBuilder CreateBuilder() => new PurchaseOrderBuilder();
    internal sealed class PurchaseOrderBuilder
    {
        private Guid? id;
        private string? code;
        private Guid? vendorId;
        private Guid? warehouseId;

        private IGetByIdService<PurchaseOrder>? purchaseOrderByIdGetter;
        private ICodeExistCheckingService? purchaseOrderCodeChecker;
        private IGetByIdService<Vendor>? vendorByIdGetter;
        private IGetByIdService<Warehouse>? warehouseByIdGetter;

        public PurchaseOrderBuilder WithId(Guid id, IGetByIdService<PurchaseOrder> purchaseOrderByIdGetter)
        {
            ArgumentNullException.ThrowIfNull(purchaseOrderByIdGetter);
            this.id = id;
            this.purchaseOrderByIdGetter = purchaseOrderByIdGetter;

            return this;
        }
        public PurchaseOrderBuilder WithCode(string code, ICodeExistCheckingService codeChecker)
        {
            ArgumentException.ThrowIfNullOrEmpty(code);
            ArgumentNullException.ThrowIfNull(codeChecker);
            this.code = code;
            this.purchaseOrderCodeChecker = codeChecker;

            return this;
        }
        public PurchaseOrderBuilder WithVendor(Guid vendorId, IGetByIdService<Vendor> vendorByIdGetter)
        {
            ArgumentNullException.ThrowIfNull(vendorByIdGetter);
            this.vendorId = vendorId;
            this.vendorByIdGetter = vendorByIdGetter;

            return this;
        }
        public PurchaseOrderBuilder WithWarehouse(Guid? warehouseId, IGetByIdService<Warehouse> warehouseByIdGetter)
        {
            ArgumentNullException.ThrowIfNull(warehouseByIdGetter);
            this.warehouseId = warehouseId;
            this.warehouseByIdGetter = warehouseByIdGetter;

            return this;
        }

        public Task<PurchaseOrder> BuildAsync(IGetByIdService<PurchaseOrder> byIdGetter, ICurrentUserAccessor currentUserAccessor)
        {
            id = Guid.NewGuid();
            purchaseOrderByIdGetter = byIdGetter;
            return BuildAsync(currentUserAccessor);
        }

        public async Task<PurchaseOrder> BuildAsync(ICurrentUserAccessor currentUserAccessor)
        {
            if (!id.HasValue)
                throw new PurchaseOrderDataIsInvalidException("Error.DataInvalid.Required", "Id");
            if (!vendorId.HasValue)
                throw new PurchaseOrderDataIsInvalidException("Error.DataInvalid.Required", "VendorId");

            ArgumentNullException.ThrowIfNull(purchaseOrderByIdGetter);
            ArgumentNullException.ThrowIfNull(purchaseOrderCodeChecker);
            ArgumentNullException.ThrowIfNull(vendorByIdGetter);
            ArgumentNullException.ThrowIfNull(currentUserAccessor);
            if (warehouseId.HasValue)
                ArgumentNullException.ThrowIfNull(warehouseByIdGetter);

            var purchaseOrder = await purchaseOrderByIdGetter.GetByIdAsync(id.Value).ConfigureAwait(false);
            if (purchaseOrder is not null)
                throw new PurchaseOrdersIdIsExistingException(id.Value);

            if (await purchaseOrderCodeChecker.DoesCodeExistAsync(code!).ConfigureAwait(false))
                throw new PurchaseOrderCodeExistsException(code!);

            var vendor = await vendorByIdGetter.GetByIdAsync(vendorId.Value).ConfigureAwait(false);
            if (vendor is null)
                throw new VendorIsNotFoundException(vendorId.Value);

            if (warehouseId.HasValue)
            {
                var warehouse = await warehouseByIdGetter!.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
                if (warehouse is null)
                    throw new WarehouseIsNotFoundException(warehouseId.Value);
            }

            var currentUser = await currentUserAccessor.GetCurrentUserAsync();
            return new PurchaseOrder(id.Value, code!, vendorId.Value, warehouseId, currentUser);
        }
    }

    #endregion
}

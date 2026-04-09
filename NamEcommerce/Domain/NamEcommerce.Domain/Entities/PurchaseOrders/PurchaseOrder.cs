using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;

namespace NamEcommerce.Domain.Entities.PurchaseOrders;

[Serializable]
public sealed record PurchaseOrder : AppAggregateEntity
{
    internal PurchaseOrder(string code, Guid? vendorId, Guid? warehouseId, Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        VendorId = vendorId;
        WarehouseId = warehouseId;
        CreatedByUserId = createdByUserId;

        Status = PurchaseOrderStatus.Draft;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; }

    public Guid? VendorId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public Guid? CreatedByUserId { get; }

    public PurchaseOrderStatus Status { get; private set; }

    public DateTime? ExpectedDeliveryDateUtc { get; internal set; }

    public string? Note { get; internal set; }

    public decimal Subtotal => _items.Sum(x => x.TotalCost);
    public decimal TaxAmount { get; internal set; }
    public decimal ShippingAmount { get; internal set; }
    public decimal TotalAmount => Subtotal + TaxAmount + ShippingAmount;

    private readonly List<PurchaseOrderItem> _items = [];
    public IEnumerable<PurchaseOrderItem> Items => _items.AsReadOnly();

    public DateTime CreatedOnUtc { get; }
    public DateTime? UpdatedOnUtc { get; internal set; }

    #region Methods

    internal async Task ChangeVendorAsync(Guid? vendorId, IGetByIdService<Vendor> byIdGetter)
    {
        if (VendorId == vendorId)
            return;

        if (!vendorId.HasValue)
        {
            RemoveVendor();
            return;
        }

        ArgumentNullException.ThrowIfNull(byIdGetter);

        var vendor = await byIdGetter.GetByIdAsync(vendorId.Value).ConfigureAwait(false);
        if (vendor is null)
            throw new VendorIsNotFoundException(vendorId.Value);

        VendorId = vendorId;
    }
    internal void RemoveVendor() => VendorId = null;

    internal async Task ChangeWarehouse(Guid? warehouseId, IGetByIdService<Warehouse> byIdGetter)
    {
        if (WarehouseId == warehouseId)
            return;

        if (!warehouseId.HasValue)
        {
            RemoveVendor();
            return;
        }

        ArgumentNullException.ThrowIfNull(byIdGetter);

        var warehouse = await byIdGetter.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
        if (warehouse is null)
            throw new WarehouseIsNotFoundException(warehouseId.Value);

        WarehouseId = warehouseId;
    }
    internal void RemoveWarehouse() => WarehouseId = null;

    internal async Task AddPurchaseOrderItemAsync(PurchaseOrderItem item, IGetByIdService<Product> byIdGetter)
    {
        if (item.PurchaseOrderId != Id)
            throw new InvalidOperationException("The item does not belong to this purchase order.");

        ArgumentNullException.ThrowIfNull(byIdGetter);

        var product = await byIdGetter.GetByIdAsync(item.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(item.ProductId);

        _items.Add(item);
    }

    public bool CanAddPurchaseOrderItems() => Status == PurchaseOrderStatus.Draft;
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

    public bool CanReceiveGoods() => Status == PurchaseOrderStatus.Approved || Status == PurchaseOrderStatus.Receiving;

    internal void ChangeStatus(PurchaseOrderStatus status)
    {
        if (!CanChangeStatusTo(status))
            throw new PurchaseOrderCannotChangeStatusException();

        Status = status;
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

    #endregion
}

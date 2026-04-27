using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Events.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.Media;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Domain.Entities.GoodsReceipts;

[Serializable]
public sealed record GoodsReceipt : AppAggregateEntity
{
    private GoodsReceipt() : base(Guid.Empty) { }

    private GoodsReceipt(Guid id, CurrentUserInfoDto? createdByUser) : base(id)
    {
        CreatedByUserId = createdByUser?.Id;
        CreatedByUsername = createdByUser?.Username;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public DateTime ReceivedOnUtc { get; private set; }

    public Guid? PurchaseOrderId { get; private set; }
    public string? PurchaseOrderCode { get; private set; }

    public string? TruckDriverName
    {
        get;
        internal set
        {
            field = value;
            TruckDriverNameNormalized = TextHelper.Normalize(value);
        }
    }
    internal string? TruckDriverNameNormalized { get; private set; }
    public string? TruckNumberSerial { get; internal set; }

    private readonly IList<GoodsReceiptItem> _items = [];
    public IReadOnlyCollection<GoodsReceiptItem> Items => _items.AsReadOnly();

    private readonly IList<Guid> _pictureIds = [];
    public IReadOnlyCollection<Guid> PictureIds => _pictureIds.AsReadOnly();

    public string? Note { get; internal set; }

    public Guid? VendorId { get; private set; }
    public string? VendorName { get; private set; }
    public string? VendorPhone { get; private set; }
    public string? VendorAddress { get; private set; }

    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByUsername { get; private set; }
    public DateTime CreatedOnUtc { get; set; }

    #region Methods

    public bool IsPendingCosting() => Items.Any(item => item.IsPendingCosting());

    internal async Task AddItemAsync(Guid productId, Guid? warehouseId, decimal quantity, decimal? unitCost,
        IGetByIdService<Product> productByIdGetter, WarehouseSettings warehouseSettings, IGetByIdService<Warehouse> warehouseByIdGetter)
    {
        var hasSameProductAndWarehouse = Items.Any(item => item.ProductId == productId && item.WarehouseId == warehouseId);
        if (hasSameProductAndWarehouse)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.Item.SameProductAndWarehouseExisting");

        var item = await GoodsReceiptItem.CreateAsync(productId, warehouseId, quantity, unitCost, productByIdGetter, warehouseSettings, warehouseByIdGetter).ConfigureAwait(false);
        _items.Add(item);
    }

    internal void ItemUnitCost(Guid itemId, decimal unitCost)
    {
        var item = Items.FirstOrDefault(item => item.Id == itemId);
        if (item is null)
            throw new GoodsReceiptItemIsNotFoundException(itemId);

        if (!item.IsPendingCosting())
            throw new GoodsReceiptItemCannotSetUnitCostException();

        item.SetUnitCost(unitCost);
    }

    internal void SetReceivedDate(DateTime receivedOnUtc)
    {
        if (receivedOnUtc > DateTime.UtcNow)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.ReceivedDateGreaterThanNow");
        ReceivedOnUtc = receivedOnUtc;
    }

    internal void SetToPurchaseOrder(Guid purchaseOrderId, string purchaseOrderCode)
    {
        PurchaseOrderId = purchaseOrderId;
        PurchaseOrderCode = purchaseOrderCode;

        MarkSetToPurchaseOrder(purchaseOrderId);
    }
    internal void RemoveFromPurchaseOrder()
    {
        if (!PurchaseOrderId.HasValue)
            return;

        var purchaseOrderId = PurchaseOrderId.Value;
        PurchaseOrderId = null;

        MarkRemovedFromPurchaseOrder(purchaseOrderId);
    }

    internal void SetVendor(Guid vendorId, string vendorName, string? vendorPhone, string? vendorAddress)
    {
        if (vendorId == Guid.Empty)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.VendorIdRequired");

        VendorId = vendorId;
        VendorName = vendorName;
        VendorPhone = vendorPhone;
        VendorAddress = vendorAddress;
    }

    internal void ClearVendor()
    {
        VendorId = null;
        VendorName = null;
        VendorPhone = null;
        VendorAddress = null;
    }

    internal void ClearPictures() => _pictureIds.Clear();
    internal async Task AddPictureAsync(Guid pictureId, IGetByIdService<Picture> pictureByIdGetter)
    {
        var picture = await pictureByIdGetter.GetByIdAsync(pictureId).ConfigureAwait(false);
        if (picture is null)
            throw new PictureIsNotFoundException(pictureId);

        _pictureIds.Add(pictureId);
    }

    internal static async Task<GoodsReceipt> CreateAsync(Guid id, IGetByIdService<GoodsReceipt> byIdGetter, ICurrentUserAccessor currentUserAccessor)
    {
        ArgumentNullException.ThrowIfNull(byIdGetter);
        ArgumentNullException.ThrowIfNull(currentUserAccessor);

        var sameIdGoodsReceipt = await byIdGetter.GetByIdAsync(id).ConfigureAwait(false);
        if (sameIdGoodsReceipt is not null)
            throw new GoodsReceiptIdIsExistingException(id);

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var goodsReceipt = new GoodsReceipt(id, currentUser);

        return goodsReceipt;
    }

    #endregion


    #region Events

    private void MarkSetToPurchaseOrder(Guid purchaseOrderId) => RaiseDomainEvent(new GoodsReceiptSetToPurchaseOrder(Id, purchaseOrderId));
    private void MarkRemovedFromPurchaseOrder(Guid purchaseOrderId) => RaiseDomainEvent(new GoodsReceiptRemovedFromPurchaseOrder(Id, purchaseOrderId));

    #endregion
}
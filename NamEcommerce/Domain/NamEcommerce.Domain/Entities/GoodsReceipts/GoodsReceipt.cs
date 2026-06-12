using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Metadata;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Enums.GoodsReceipts;
using NamEcommerce.Domain.Shared.Events.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.Media;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Domain.Shared.Settings;
using NamEcommerce.Domain.Values;

namespace NamEcommerce.Domain.Entities.GoodsReceipts;

[Serializable]
public sealed record GoodsReceipt : AppAggregateEntity
{
    public const string CODE_PREFIX = "PN";

    private GoodsReceipt() : base(Guid.Empty) { Code = string.Empty; }

    private GoodsReceipt(Guid id, CurrentUserInfoDto? createdByUser) : base(id)
    {
        Code = string.Empty;
        CreatedByUserId = createdByUser?.Id;
        CreatedByUsername = createdByUser?.Username;
        CreatedOnUtc = DateTime.UtcNow;
    }

    private GoodsReceipt(Guid id, Guid? createdByUserId, string? createdByUsername = null) : base(id)
    {
        Code = string.Empty;
        CreatedByUserId = createdByUserId;
        CreatedByUsername = createdByUsername;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; }

    internal void SetCode(string code) => Code = code;

    public DateTime ReceivedOnUtc { get; private set; }

    public GoodsReceiptSourceType SourceType { get; internal set; } = GoodsReceiptSourceType.FromVendor;

    public Guid? PurchaseOrderId { get; private set; }
    public string? PurchaseOrderCode { get; private set; }

    public Guid? BulkReceiveBatchId { get; private set; }

    internal void SetBulkReceiveBatchId(Guid batchId) => BulkReceiveBatchId = batchId;

    public NormalizableString TruckDriverName { get; internal set; }
    public string? TruckNumberSerial { get; internal set; }

    private readonly IList<GoodsReceiptItem> _items = [];
    public IReadOnlyCollection<GoodsReceiptItem> Items => _items.AsReadOnly();

    private readonly IList<Guid> _pictureIds = [];
    public IReadOnlyCollection<Guid> PictureIds => _pictureIds.AsReadOnly();

    public string? Note { get; internal set; }

    public Guid? VendorId { get; private set; }
    internal VendorInfo? VendorInfo { get; private set; }

    // PRE-4b: Thuế GTGT đầu vào — computed từ items
    public decimal TotalTaxAmount => _items.Sum(i => i.TaxAmount);

    // PRE-5: Số hóa đơn từ nhà cung cấp
    public string? VendorInvoiceNumber { get; internal set; }
    public DateTime? VendorInvoiceDate { get; internal set; }

    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByUsername { get; private set; }
    public DateTime CreatedOnUtc { get; set; }

    #region Methods

    public bool IsPendingCosting() => Items.Any(item => item.IsPendingCosting());

    internal async Task AddItemAsync(Guid productId, Guid? warehouseId, decimal quantity, decimal? unitCost,
        IGetByIdService<Product> productByIdGetter, WarehouseSettings warehouseSettings, IGetByIdService<Warehouse> warehouseByIdGetter)
    {
        var item = await GoodsReceiptItem.CreateAsync(productId, warehouseId, quantity, unitCost, productByIdGetter, warehouseSettings, warehouseByIdGetter).ConfigureAwait(false);
        _items.Add(item);
    }

    internal void AddItem(GoodsReceiptItem item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        _items.Add(item);
    }

    internal void SplitToNewItemWithQuantity(Guid itemId, decimal quantity)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            throw new GoodsReceiptItemIsNotFoundException(itemId);

        if (quantity > item.Quantity)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.Item.SplitQuantityTooLarge");

        if (quantity == item.Quantity)
            return;

        item.Quantity -= quantity;
        var newItem = item.CopyWithQuantity(quantity);

        _items.Add(newItem);
    }

    internal void RaiseItemSplitOnLinking(Guid purchaseOrderId, Guid originalItemId, Guid productId, decimal splitQuantity, decimal assignedUnitCost)
        => RaiseDomainEvent(new GoodsReceiptItemSplitOnLinking(Id, purchaseOrderId, originalItemId, productId, splitQuantity, assignedUnitCost));

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
        VendorInfo = new VendorInfo(vendorName, vendorPhone, vendorAddress);
    }

    internal void ClearVendor()
    {
        VendorId = null;
        VendorInfo = null;
    }

    internal void ClearPictures() => _pictureIds.Clear();
    internal async Task AddPictureAsync(Guid pictureId, IGetByIdService<Picture> pictureByIdGetter)
    {
        var picture = await pictureByIdGetter.GetByIdAsync(pictureId, default).ConfigureAwait(false);
        if (picture is null)
            throw new PictureIsNotFoundException(pictureId);

        _pictureIds.Add(pictureId);
    }

    internal static async Task<GoodsReceipt> CreateAsync(Guid id, ICurrentUserAccessor currentUserAccessor, IGetByIdService<GoodsReceipt> byIdGetter)
    {
        ArgumentNullException.ThrowIfNull(byIdGetter);
        ArgumentNullException.ThrowIfNull(currentUserAccessor);

        var sameIdGoodsReceipt = await byIdGetter.GetByIdAsync(id, default).ConfigureAwait(false);
        if (sameIdGoodsReceipt is not null)
            throw new GoodsReceiptIdIsExistingException(id);

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        return await CreateAsync(id, currentUser, byIdGetter).ConfigureAwait(false);
    }
    internal static async Task<GoodsReceipt> CreateAsync(Guid id, CurrentUserInfoDto? createdByUser, IGetByIdService<GoodsReceipt> byIdGetter)
    {
        ArgumentNullException.ThrowIfNull(byIdGetter);

        var sameIdGoodsReceipt = await byIdGetter.GetByIdAsync(id, default).ConfigureAwait(false);
        if (sameIdGoodsReceipt is not null)
            throw new GoodsReceiptIdIsExistingException(id);

        var goodsReceipt = new GoodsReceipt(id, createdByUser);

        return goodsReceipt;
    }

    #endregion

    #region Events

    private void MarkSetToPurchaseOrder(Guid purchaseOrderId) => RaiseDomainEvent(new GoodsReceiptSetToPurchaseOrder(Id, purchaseOrderId));

    private void MarkRemovedFromPurchaseOrder(Guid purchaseOrderId) => RaiseDomainEvent(new GoodsReceiptRemovedFromPurchaseOrder(Id, purchaseOrderId));

    internal void MarkCreated() => RaiseDomainEvent(new GoodsReceiptCreated(Id));

    internal void MarkUpdated() => RaiseDomainEvent(new GoodsReceiptUpdated(Id));

    internal void MarkItemUnitCostSet(Guid itemId) => RaiseDomainEvent(new GoodsReceiptItemUnitCostSet(Id, itemId));

    internal void MarkVendorChanged() => RaiseDomainEvent(new GoodsReceiptVendorChanged(Id));

    internal void MarkDeleted() => RaiseDomainEvent(new GoodsReceiptDeleted(Id, _pictureIds.ToList().AsReadOnly()));

    #endregion
}

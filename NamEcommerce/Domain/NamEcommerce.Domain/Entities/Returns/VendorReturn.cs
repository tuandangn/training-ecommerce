using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Returns;
using NamEcommerce.Domain.Shared.Events.Returns;
using NamEcommerce.Domain.Shared.Exceptions.Returns;

namespace NamEcommerce.Domain.Entities.Returns;

[Serializable]
public sealed record VendorReturn : AppAggregateEntity
{
    private VendorReturn() : base(Guid.Empty) { }

    internal VendorReturn(string code, Guid vendorId, string vendorName,
        Guid? purchaseOrderId, Guid? goodsReceiptId,
        Guid warehouseId, string warehouseName,
        string? note, Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        VendorId = vendorId;
        VendorName = vendorName;
        PurchaseOrderId = purchaseOrderId;
        GoodsReceiptId = goodsReceiptId;
        WarehouseId = warehouseId;
        WarehouseName = warehouseName;
        Note = note;
        CreatedByUserId = createdByUserId;
        Status = VendorReturnStatus.Draft;
        ReturnDate = DateTime.UtcNow;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; } = string.Empty;

    public Guid VendorId { get; private set; }
    public string VendorName { get; private set; } = string.Empty;

    /// <summary>Đơn nhập gốc — nullable nếu trả từ GoodsReceipt độc lập (không qua PO).</summary>
    public Guid? PurchaseOrderId { get; private set; }

    /// <summary>Phiếu nhập kho gốc — nullable nếu trả theo PO (không cần chỉ định GoodsReceipt cụ thể).</summary>
    public Guid? GoodsReceiptId { get; private set; }

    public Guid WarehouseId { get; private set; }
    public string WarehouseName { get; private set; } = string.Empty;

    public string? Note { get; internal set; }
    public VendorReturnStatus Status { get; private set; }
    public DateTime ReturnDate { get; internal set; }

    public DateTime? ConfirmedOnUtc { get; private set; }

    /// <summary>ID phiếu xuất kho được tự động sinh khi Confirm (SourceType=ToVendorReturn, Status=Delivered).</summary>
    public Guid? GeneratedDeliveryNoteId { get; internal set; }

    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    private readonly IList<VendorReturnItem> _items = [];
    public IReadOnlyCollection<VendorReturnItem> Items => _items.AsReadOnly();

    #region Methods

    internal void AddItem(Guid productId, string productName, Guid? goodsReceiptItemId,
        decimal requestedQuantity, decimal acceptedQuantity, decimal unitCost)
    {
        if (requestedQuantity <= 0)
            throw new ReturnDataIsInvalidException("Error.VendorReturn.RequestedQuantityMustBePositive");
        if (acceptedQuantity < 0)
            throw new ReturnDataIsInvalidException("Error.VendorReturn.AcceptedQuantityCannotBeNegative");

        var item = new VendorReturnItem(Guid.NewGuid(), Id, productId, productName,
            goodsReceiptItemId, requestedQuantity, acceptedQuantity, unitCost);
        _items.Add(item);
    }

    internal void MoveToInspecting()
    {
        if (Status != VendorReturnStatus.Draft)
            throw new ReturnCannotChangeStatusException(Status.ToString(), nameof(VendorReturnStatus.Inspecting));

        Status = VendorReturnStatus.Inspecting;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Confirm()
    {
        if (Status != VendorReturnStatus.Inspecting)
            throw new ReturnCannotChangeStatusException(Status.ToString(), nameof(VendorReturnStatus.Confirmed));

        if (!_items.Any())
            throw new ReturnDataIsInvalidException("Error.VendorReturn.NoItems");

        Status = VendorReturnStatus.Confirmed;
        ConfirmedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new VendorReturnConfirmed(Id, PurchaseOrderId, GoodsReceiptId, VendorId, WarehouseId));
    }

    internal void Cancel()
    {
        if (Status is VendorReturnStatus.Confirmed)
            throw new ReturnCannotChangeStatusException(Status.ToString(), nameof(VendorReturnStatus.Cancelled));

        Status = VendorReturnStatus.Cancelled;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new VendorReturnCancelled(Id));
    }

    internal void MarkCreated() { /* no event needed at Draft creation */ }

    #endregion
}

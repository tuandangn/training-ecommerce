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
        Guid? warehouseId, string? warehouseName,
        string? note, decimal additionalCost,
        Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        VendorId = vendorId;
        VendorName = vendorName;
        PurchaseOrderId = purchaseOrderId;
        GoodsReceiptId = goodsReceiptId;
        WarehouseId = warehouseId ?? Guid.Empty;
        WarehouseName = warehouseName ?? string.Empty;
        Note = note;
        AdditionalCost = additionalCost;
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

    /// <summary>Phiếu nhập kho gốc — nullable nếu trả theo PO hoặc tạo tự do.</summary>
    public Guid? GoodsReceiptId { get; private set; }

    public Guid WarehouseId { get; private set; }
    public string WarehouseName { get; private set; } = string.Empty;

    public string? Note { get; internal set; }
    public VendorReturnStatus Status { get; private set; }
    public DateTime ReturnDate { get; internal set; }

    public DateTime? ConfirmedOnUtc { get; private set; }
    public DateTime? ReversedOnUtc { get; private set; }
    public string? ReversedReason { get; private set; }

    public Guid? InspectedByUserId { get; private set; }
    public DateTime? InspectedOnUtc { get; private set; }

    /// <summary>Chi phí phát sinh khi trả hàng cho NCC (vận chuyển, đền bù...). Tự động ghi Expense khi Confirm.</summary>
    public decimal AdditionalCost { get; private set; }

    /// <summary>ID phiếu xuất kho được tự động sinh khi Confirm (SourceType=ToVendorReturn, Status=Delivered).</summary>
    public Guid? GeneratedDeliveryNoteId { get; internal set; }

    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    private readonly IList<VendorReturnItem> _items = [];
    public IReadOnlyCollection<VendorReturnItem> Items => _items.AsReadOnly();

    #region Methods

    internal void AddItem(Guid productId, string productName, Guid? goodsReceiptItemId,
        decimal requestedQuantity, decimal acceptedQuantity,
        decimal? originalUnitCost, decimal returnUnitCost)
    {
        if (requestedQuantity <= 0)
            throw new ReturnDataIsInvalidException("Error.VendorReturn.RequestedQuantityMustBePositive");
        if (acceptedQuantity < 0)
            throw new ReturnDataIsInvalidException("Error.VendorReturn.AcceptedQuantityCannotBeNegative");
        if (returnUnitCost < 0)
            throw new ReturnDataIsInvalidException("Error.VendorReturn.ReturnUnitCostCannotBeNegative");

        var item = new VendorReturnItem(Id, productId, productName,
            goodsReceiptItemId, requestedQuantity, acceptedQuantity, originalUnitCost, returnUnitCost);
        _items.Add(item);
    }

    internal void MoveToInspecting(Guid? inspectorUserId)
    {
        if (Status != VendorReturnStatus.Draft)
            throw new ReturnCannotChangeStatusException(Status.ToString(), nameof(VendorReturnStatus.Inspecting));

        Status = VendorReturnStatus.Inspecting;
        InspectedByUserId = inspectorUserId;
        InspectedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void SetWarehouse(Guid warehouseId, string warehouseName)
    {
        if (warehouseId == Guid.Empty)
            throw new ReturnDataIsInvalidException("Error.VendorReturn.WarehouseRequired");

        WarehouseId = warehouseId;
        WarehouseName = warehouseName;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Confirm()
    {
        if (Status != VendorReturnStatus.Inspecting)
            throw new ReturnCannotChangeStatusException(Status.ToString(), nameof(VendorReturnStatus.Confirmed));

        if (!_items.Any())
            throw new ReturnDataIsInvalidException("Error.VendorReturn.NoItems");
        if (WarehouseId == Guid.Empty)
            throw new ReturnDataIsInvalidException("Error.VendorReturn.WarehouseRequired");

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

    internal void MarkReversed(string reason)
    {
        if (Status != VendorReturnStatus.Confirmed)
            throw new ReturnCannotChangeStatusException(Status.ToString(), nameof(VendorReturnStatus.Reversed));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ReturnDataIsInvalidException("Error.VendorReturn.ReverseReasonRequired");

        Status = VendorReturnStatus.Reversed;
        ReversedReason = reason;
        ReversedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void MarkCreated() { /* no event needed at Draft creation */ }

    internal void MarkOverRecovered(decimal overAmount)
        => RaiseDomainEvent(new VendorReturnOverRecovered(Id, VendorId, overAmount));

    #endregion
}

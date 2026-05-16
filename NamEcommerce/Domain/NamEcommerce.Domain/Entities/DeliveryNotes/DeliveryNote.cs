using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;

namespace NamEcommerce.Domain.Entities.DeliveryNotes;

[Serializable]
public sealed record DeliveryNote : AppAggregateEntity
{
    public DeliveryNote(Guid id) : base(id)
    {
        Code = string.Empty;
        CustomerName = string.Empty;
        ShippingAddress = string.Empty;
        _items = [];
    }

    /// <summary>
    /// Constructor dùng cho DeliveryNote tự sinh khi VendorReturn.Confirmed —
    /// không có Order/Customer. SourceType phải được set thành <see cref="DeliveryNoteSourceType.ToVendorReturn"/> ngay sau.
    /// </summary>
    internal DeliveryNote(string code, Guid warehouseId, string? note, Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        WarehouseId = warehouseId;
        Note = note;
        CreatedByUserId = createdByUserId;
        Status = DeliveryNoteStatus.Draft;
        CreatedOnUtc = DateTime.UtcNow;
        // Sentinel values — không liên quan đến Order hay Customer
        OrderId = Guid.Empty;
        CustomerId = Guid.Empty;
        CustomerName = string.Empty;
        ShippingAddress = string.Empty;
        _items = [];
    }

    internal DeliveryNote(string code, Guid orderId,
        Guid customerId, string customerName, string? customerPhone, string? customerAddress,
        string shippingAddress, Guid warehouseId, bool showPrice, string? note,
        decimal surcharge, decimal amountToCollect, string? surchargeReason,
        Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        OrderId = orderId;
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerPhone = customerPhone;
        CustomerAddress = customerAddress;
        ShippingAddress = shippingAddress;
        ShowPrice = showPrice;
        Note = note;
        Surcharge = surcharge;
        AmountToCollect = amountToCollect;
        SurchargeReason = surchargeReason;
        Status = DeliveryNoteStatus.Draft;
        CreatedByUserId = createdByUserId;
        WarehouseId = warehouseId;
        CreatedOnUtc = DateTime.UtcNow;
        _items = [];
    }

    public string Code { get; private set; }

    public Guid OrderId { get; private set; }
    public string? OrderCode { get; set; }

    public Guid WarehouseId { get; private set; }
    public string? WarehouseName { get; internal set; }

    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; }
    public string? CustomerPhone { get; private set; }
    public string? CustomerAddress { get; private set; }
    
    public string ShippingAddress { get; private set; }
    
    public bool ShowPrice { get; private set; }
    public string? Note { get; private set; }
    
    public decimal Surcharge { get; internal set; }
    public string? SurchargeReason { get; internal set; }
    public decimal AmountToCollect { get; internal set; }
    
    public DeliveryNoteStatus Status { get; private set; }

    /// <summary>
    /// Nguồn gốc phiếu xuất. Mặc định <see cref="DeliveryNoteSourceType.ToCustomer"/> (xuất bán cho khách).
    /// Phase B sẽ thêm <see cref="DeliveryNoteSourceType.ToVendorReturn"/> khi VendorReturn.Confirmed
    /// auto-sinh phiếu xuất loại này. Handler downstream phân nhánh theo property này (ví dụ:
    /// <c>DeliveryNoteDeliveredEventHandler</c> skip sinh <c>CustomerDebt</c> khi không phải <c>ToCustomer</c>).
    /// </summary>
    public DeliveryNoteSourceType SourceType { get; internal set; } = DeliveryNoteSourceType.ToCustomer;

    // Direct-ship fields
    /// <summary>True khi phiếu này được sinh tự động từ workflow direct-ship.</summary>
    public bool IsDirectShip { get; private set; }
    /// <summary>Trạng thái xác nhận giao thẳng. NotApplicable cho DN thường.</summary>
    public DeliveryConfirmationStatus DeliveryConfirmationStatus { get; private set; } = DeliveryConfirmationStatus.NotApplicable;
    public DateTime? ConfirmedAtUtc { get; private set; }
    public string? ConfirmedNote { get; private set; }
    /// <summary>GoodsReceipt đã tạo ra DN direct-ship này (null với DN thường từ SO).</summary>
    public Guid? SourceGoodsReceiptId { get; private set; }

    public DateTime? DeliveredOnUtc { get; private set; }
    public Guid? DeliveryProofPictureId { get; private set; }
    public string? DeliveryReceiverName { get; private set; }
    
    public Guid? CreatedByUserId { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    private readonly List<DeliveryNoteItem> _items;
    public IReadOnlyCollection<DeliveryNoteItem> Items => _items.AsReadOnly();

    public decimal TotalAmount => _items.Sum(i => i.SubTotal);

    internal void AddItem(Guid orderItemId, Guid productId, string productName, decimal quantity, decimal unitPrice)
    {
        _items.Add(new DeliveryNoteItem(Id, orderItemId, productId, productName, quantity, unitPrice));
    }

    /// <summary>
    /// Thêm item không gắn với OrderItem — dùng khi auto-sinh DeliveryNote từ VendorReturn.
    /// OrderItemId = Guid.Empty (sentinel). UnitPrice = UnitCost của VendorReturnItem.
    /// </summary>
    internal void AddItemFromVendorReturn(Guid productId, string productName, decimal quantity, decimal unitCost)
    {
        _items.Add(new DeliveryNoteItem(Id, Guid.Empty, productId, productName, quantity, unitCost));
    }

    /// <summary>
    /// Đánh dấu Delivered ngay (không qua Draft→Confirmed→Delivering) —
    /// chỉ dùng khi auto-sinh từ VendorReturn.Confirmed. Raise <see cref="DeliveryNoteDelivered"/>
    /// để handler downstream trừ tồn kho (SourceType guard sẽ bỏ qua sinh CustomerDebt).
    /// </summary>
    internal void MarkAsDeliveredFromVendorReturn()
    {
        Status = DeliveryNoteStatus.Delivered;
        DeliveredOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new DeliveryNoteDelivered(Id, OrderId, CustomerId, TotalAmount));
    }

    /// <summary>
    /// Đánh dấu phiếu vừa được khởi tạo (status = Draft) — Manager gọi sau khi setup items để raise <see cref="DeliveryNoteCreated"/>.
    /// </summary>
    internal void MarkCreated()
        => RaiseDomainEvent(new DeliveryNoteCreated(Id, OrderId, CustomerId));

    internal void Confirm()
    {
        if (Status != DeliveryNoteStatus.Draft)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Confirmed);

        Status = DeliveryNoteStatus.Confirmed;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new DeliveryNoteConfirmed(Id));
    }

    internal void MarkDelivering()
    {
        if (Status != DeliveryNoteStatus.Confirmed)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Delivering);

        Status = DeliveryNoteStatus.Delivering;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new DeliveryNoteDelivering(Id));
    }

    internal void MarkDelivered(Guid pictureId, string? receiverName)
    {
        if (Status != DeliveryNoteStatus.Delivering && Status != DeliveryNoteStatus.Confirmed)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Delivered);

        if (pictureId == Guid.Empty)
            throw new DeliveryProofRequiredException();

        Status = DeliveryNoteStatus.Delivered;
        DeliveredOnUtc = DateTime.UtcNow;
        DeliveryProofPictureId = pictureId;
        DeliveryReceiverName = receiverName;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new DeliveryNoteDelivered(Id, OrderId, CustomerId, TotalAmount));
    }

    internal void Cancel()
    {
        if (Status == DeliveryNoteStatus.Delivered)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Cancelled);

        var wasReservingStock = Status == DeliveryNoteStatus.Confirmed || Status == DeliveryNoteStatus.Delivering;

        Status = DeliveryNoteStatus.Cancelled;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new DeliveryNoteCancelled(Id, wasReservingStock));
    }

    /// <summary>
    /// Gắn metadata direct-ship khi Manager tạo DN từ workflow giao thẳng.
    /// Chỉ gọi ngay sau constructor, trước MarkCreated.
    /// </summary>
    internal void SetAsDirectShip(Guid sourceGoodsReceiptId)
    {
        IsDirectShip = true;
        SourceGoodsReceiptId = sourceGoodsReceiptId;
        DeliveryConfirmationStatus = DeliveryConfirmationStatus.PendingConfirmation;
    }

    /// <summary>
    /// Khách xác nhận đã nhận hàng giao thẳng — raise DirectShipDeliveryConfirmedEvent để trigger Invoice/Debt.
    /// </summary>
    internal void ConfirmDirectShipDelivery(DateTime confirmedAtUtc, string? note)
    {
        if (!IsDirectShip)
            throw new DeliveryNoteCannotChangeStatusException(Status, Status);
        if (DeliveryConfirmationStatus != DeliveryConfirmationStatus.PendingConfirmation)
            throw new DeliveryNoteCannotChangeStatusException(Status, Status);

        DeliveryConfirmationStatus = DeliveryConfirmationStatus.Confirmed;
        ConfirmedAtUtc = confirmedAtUtc;
        ConfirmedNote = note;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new DirectShipDeliveryConfirmed(Id, OrderId, CustomerId, TotalAmount));
    }

    /// <summary>
    /// Reject — hàng sẽ chuyển về kho chính (handler DirectShipDeliveryRejectedEvent xử lý stock movement).
    /// </summary>
    internal void RejectDirectShipDelivery(string reason)
    {
        if (!IsDirectShip)
            throw new DeliveryNoteCannotChangeStatusException(Status, Status);
        if (DeliveryConfirmationStatus != DeliveryConfirmationStatus.PendingConfirmation)
            throw new DeliveryNoteCannotChangeStatusException(Status, Status);

        DeliveryConfirmationStatus = DeliveryConfirmationStatus.Rejected;
        ConfirmedNote = reason;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new DirectShipDeliveryRejected(Id, OrderId, SourceGoodsReceiptId));
    }
}

using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
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

    internal void Confirm()
    {
        if (Status != DeliveryNoteStatus.Draft)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Confirmed);

        Status = DeliveryNoteStatus.Confirmed;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void MarkDelivering()
    {
        if (Status != DeliveryNoteStatus.Confirmed)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Delivering);

        Status = DeliveryNoteStatus.Delivering;
        UpdatedOnUtc = DateTime.UtcNow;
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
    }

    internal void Cancel()
    {
        if (Status == DeliveryNoteStatus.Delivered)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Cancelled);

        Status = DeliveryNoteStatus.Cancelled;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}

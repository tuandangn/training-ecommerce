using NamEcommerce.Domain.Metadata;
using System.Linq;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Events.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;
using NamEcommerce.Domain.Values;

namespace NamEcommerce.Domain.Entities.DeliveryNotes;

[Serializable]
public sealed record DeliveryNote : AppAggregateEntity
{
    public const string CODE_PREFIX = "PX";

    public DeliveryNote(Guid id) : base(id)
    {
        Code = string.Empty;
        CustomerInfo = new CustomerInfo(string.Empty, string.Empty, string.Empty);
        ShippingAddress = string.Empty;
        _items = [];
    }

    internal DeliveryNote(string code, Guid warehouseId, string? note, Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        WarehouseId = warehouseId;
        Note = note;
        CreatedByUserId = createdByUserId;
        Status = DeliveryNoteStatus.Draft;
        CreatedOnUtc = DateTime.UtcNow;
        OrderId = Guid.Empty;
        CustomerId = Guid.Empty;
        CustomerInfo = new CustomerInfo(string.Empty, string.Empty, string.Empty);
        ShippingAddress = string.Empty;
        _items = [];
    }

    internal DeliveryNote(string code, Guid orderId,
        Guid customerId, string customerName, string customerPhone, string? customerAddress,
        string shippingAddress, Guid warehouseId, bool showPrice, string? note,
        decimal surcharge, decimal amountToCollect, string? surchargeReason,
        Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        OrderId = orderId;
        CustomerId = customerId;
        CustomerInfo = new CustomerInfo(customerName, customerPhone, customerAddress);
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
    public DeliveryNoteStatus Status { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public bool ShowPrice { get; private set; }
    public string? Note { get; private set; }

    public Guid OrderId { get; private set; }
    public string? OrderCode { get; set; }

    public Guid WarehouseId { get; private set; }
    public string? WarehouseName { get; internal set; }

    public Guid? AssignedDeliveryUserId { get; private set; }
    public string? AssignedDeliveryUsername { get; private set; }
    public string? AssignedDeliveryFullName { get; private set; }
    public DateTime? AssignedDeliveryOnUtc { get; private set; }

    public Guid CustomerId { get; private set; }
    public CustomerInfo CustomerInfo { get; private set; }
    public NormalizableString ShippingAddress { get; internal set; }
    
    public decimal Surcharge { get; internal set; }
    public string? SurchargeReason { get; internal set; }
    public decimal AmountToCollect { get; internal set; }
    public decimal TotalAmount => _items.Sum(i => i.SubTotal);

    // PRE-3: Chiết khấu thương mại — computed từ items
    public decimal TotalDiscountAmount => _items.Sum(i => i.DiscountAmount);

    // PRE-4a: Thuế GTGT — computed từ items
    public decimal TotalTaxAmount => _items.Sum(i => i.TaxAmount);

    // PRE-5: Số hóa đơn GTGT
    public string? InvoiceNumber { get; internal set; }
    public string? InvoiceSeries { get; internal set; }
    public DateTime? InvoiceDate { get; internal set; }

    private readonly List<DeliveryNoteItem> _items;
    public IReadOnlyCollection<DeliveryNoteItem> Items => _items.AsReadOnly();
    
    public DeliveryNoteSourceType SourceType { get; internal set; } = DeliveryNoteSourceType.ToCustomer;

    public bool IsDirectShip { get; private set; }
    public DeliveryConfirmationStatus DeliveryConfirmationStatus { get; private set; } = DeliveryConfirmationStatus.NotApplicable;
    public DateTime? ConfirmedAtUtc { get; private set; }
    public string? ConfirmedNote { get; private set; }
    public Guid? SourceGoodsReceiptId { get; private set; }

    public DateTime? DeliveredOnUtc { get; private set; }
    public Guid? DeliveryProofPictureId { get; private set; }
    // Comma-separated Guids stored via EF value converter; FirstOrDefault = DeliveryProofPictureId
    public IReadOnlyCollection<Guid> DeliveryProofPictureIds { get; private set; } = [];
    public string? DeliveryReceiverName { get; private set; }
    public double? DeliveryLatitude { get; private set; }
    public double? DeliveryLongitude { get; private set; }
    public string? DeliveryLocationAddress { get; private set; }
    public string? DeliveryCompletionNote { get; private set; }
    public string? DeliveryCompletionSource { get; private set; }
    public string? DeliveryCompletionIdempotencyKey { get; private set; }
    public decimal? DeliveryCashCollectedAmount { get; private set; }
    
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    #region Events

    internal void AddItem(Guid orderItemId, Guid productId, string productName, decimal quantity, decimal unitPrice)
    {
        _items.Add(new DeliveryNoteItem(Id, orderItemId, productId, productName, quantity, unitPrice));
    }

    internal void AddItem(Guid orderItemId, Guid productId, string productName, decimal quantity, decimal unitPrice, Guid warehouseId)
    {
        _items.Add(new DeliveryNoteItem(Id, orderItemId, productId, productName, quantity, unitPrice, warehouseId));
    }

    internal void AddItemFromVendorReturn(Guid productId, string productName, decimal quantity, decimal unitCost, Guid warehouseId)
    {
        _items.Add(new DeliveryNoteItem(Id, Guid.Empty, productId, productName, quantity, unitCost, warehouseId));
    }

    internal void MarkAsDeliveredFromVendorReturn()
    {
        Status = DeliveryNoteStatus.Delivered;
        DeliveredOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new DeliveryNoteDelivered(Id, OrderId, CustomerId, AmountToCollect));
    }

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

        EnsureDeliveryUserAssignedBeforeLeavingWarehouse();

        Status = DeliveryNoteStatus.Delivering;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new DeliveryNoteDelivering(Id));
    }

    internal void AssignDeliveryUser(Guid userId, string username, string fullName, DateTime assignedOnUtc)
    {
        if (userId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.DeliveryUserRequired");
        if (string.IsNullOrWhiteSpace(username))
            throw new NamEcommerceDomainException("Error.DeliveryUsernameRequired");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new NamEcommerceDomainException("Error.DeliveryFullNameRequired");
        if (IsDirectShip || SourceType != DeliveryNoteSourceType.ToCustomer)
            throw new NamEcommerceDomainException("Error.CannotAssignDeliveryUserForThisDeliveryNote");
        if (Status is DeliveryNoteStatus.Delivered or DeliveryNoteStatus.Cancelled)
            throw new NamEcommerceDomainException("Error.CannotAssignDeliveryUserAfterClosed");

        AssignedDeliveryUserId = userId;
        AssignedDeliveryUsername = username.Trim();
        AssignedDeliveryFullName = fullName.Trim();
        AssignedDeliveryOnUtc = assignedOnUtc;
        UpdatedOnUtc = assignedOnUtc;
    }

    internal void MarkDelivered(
        IReadOnlyList<Guid> pictureIds,
        string? receiverName,
        double? latitude = null,
        double? longitude = null,
        string? locationAddress = null,
        string? completionNote = null,
        string? completionSource = null,
        string? idempotencyKey = null,
        decimal? cashCollectedAmount = null)
    {
        if (Status != DeliveryNoteStatus.Delivering && Status != DeliveryNoteStatus.Confirmed)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Delivered);

        if (pictureIds is null || pictureIds.Count == 0 || pictureIds[0] == Guid.Empty)
            throw new DeliveryProofRequiredException();

        var wasConfirmed = Status == DeliveryNoteStatus.Confirmed;
        if (wasConfirmed)
            EnsureDeliveryUserAssignedBeforeLeavingWarehouse();

        Status = DeliveryNoteStatus.Delivered;
        DeliveredOnUtc = DateTime.UtcNow;
        DeliveryProofPictureId = pictureIds[0];
        DeliveryProofPictureIds = pictureIds.ToList().AsReadOnly();
        DeliveryReceiverName = receiverName;
        SetDeliveryCompletionMetadata(latitude, longitude, locationAddress, completionNote, completionSource, idempotencyKey, cashCollectedAmount);
        UpdatedOnUtc = DateTime.UtcNow;

        if (wasConfirmed)
            RaiseDomainEvent(new DeliveryNoteDelivering(Id));
        RaiseDomainEvent(new DeliveryNoteDelivered(Id, OrderId, CustomerId, AmountToCollect));
    }

    internal bool HasSameDeliveryCompletionRequest(string? idempotencyKey)
        => !string.IsNullOrWhiteSpace(idempotencyKey)
           && string.Equals(DeliveryCompletionIdempotencyKey, idempotencyKey.Trim(), StringComparison.OrdinalIgnoreCase);

    internal bool MarkReceivedByCustomer(DateTime receivedAtUtc, string? receiverName, string? note)
    {
        if (Status == DeliveryNoteStatus.Delivered)
        {
            DeliveryConfirmationStatus = DeliveryConfirmationStatus.Confirmed;
            ConfirmedAtUtc ??= receivedAtUtc;
            ConfirmedNote = note;
            DeliveryReceiverName = string.IsNullOrWhiteSpace(receiverName) ? DeliveryReceiverName : receiverName;
            UpdatedOnUtc = DateTime.UtcNow;
            return false;
        }

        if (Status != DeliveryNoteStatus.Delivering && Status != DeliveryNoteStatus.Confirmed)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Delivered);

        var wasConfirmed = Status == DeliveryNoteStatus.Confirmed;
        if (wasConfirmed)
            EnsureDeliveryUserAssignedBeforeLeavingWarehouse();

        DeliveryConfirmationStatus = DeliveryConfirmationStatus.Confirmed;
        ConfirmedAtUtc = receivedAtUtc;
        ConfirmedNote = note;
        Status = DeliveryNoteStatus.Delivered;
        DeliveredOnUtc = receivedAtUtc;
        DeliveryReceiverName = receiverName;
        UpdatedOnUtc = DateTime.UtcNow;

        if (wasConfirmed)
            RaiseDomainEvent(new DeliveryNoteDelivering(Id));
        RaiseDomainEvent(new DeliveryNoteDelivered(Id, OrderId, CustomerId, AmountToCollect));
        return true;
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

    private void EnsureDeliveryUserAssignedBeforeLeavingWarehouse()
    {
        if (RequiresDeliveryUserBeforeLeavingWarehouse() && !AssignedDeliveryUserId.HasValue)
            throw new NamEcommerceDomainException("Error.DeliveryUserRequiredBeforeLeavingWarehouse");
    }

    private bool RequiresDeliveryUserBeforeLeavingWarehouse()
        => SourceType == DeliveryNoteSourceType.ToCustomer
           && !IsDirectShip
           && !string.Equals(ShippingAddress.Value.Trim(), "Tai quay", StringComparison.OrdinalIgnoreCase);

    private void SetDeliveryCompletionMetadata(double? latitude, double? longitude, string? locationAddress,
        string? completionNote, string? completionSource, string? idempotencyKey, decimal? cashCollectedAmount)
    {
        if (cashCollectedAmount < 0)
            throw new NamEcommerceDomainException("Error.CashCollectedAmountCannotBeNegative");

        DeliveryLatitude = latitude;
        DeliveryLongitude = longitude;
        DeliveryLocationAddress = TrimToNull(locationAddress);
        DeliveryCompletionNote = TrimToNull(completionNote);
        DeliveryCompletionSource = TrimToNull(completionSource);
        DeliveryCompletionIdempotencyKey = TrimToNull(idempotencyKey);
        DeliveryCashCollectedAmount = cashCollectedAmount;
    }

    private static string? TrimToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal void SetAsDirectShip(Guid sourceGoodsReceiptId)
    {
        IsDirectShip = true;
        SourceType = DeliveryNoteSourceType.DirectShipToCustomer;
        SourceGoodsReceiptId = sourceGoodsReceiptId;
        DeliveryConfirmationStatus = DeliveryConfirmationStatus.PendingConfirmation;
    }

    internal void ConfirmDirectShipDelivery(DateTime confirmedAtUtc, string? note)
    {
        if (!IsDirectShip || SourceType != DeliveryNoteSourceType.DirectShipToCustomer)
            throw new DeliveryNoteCannotChangeStatusException(Status, Status);
        if (Status != DeliveryNoteStatus.Confirmed)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Delivered);

        DeliveryConfirmationStatus = DeliveryConfirmationStatus.Confirmed;
        ConfirmedAtUtc = confirmedAtUtc;
        ConfirmedNote = note;
        Status = DeliveryNoteStatus.Delivered;
        DeliveredOnUtc = confirmedAtUtc;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new DeliveryNoteDelivered(Id, OrderId, CustomerId, AmountToCollect));
    }

    internal void RejectDirectShipDelivery(string reason)
    {
        if (!IsDirectShip || SourceType != DeliveryNoteSourceType.DirectShipToCustomer)
            throw new DeliveryNoteCannotChangeStatusException(Status, Status);
        if (Status != DeliveryNoteStatus.Confirmed)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Cancelled);

        DeliveryConfirmationStatus = DeliveryConfirmationStatus.Rejected;
        ConfirmedNote = reason;
        UpdatedOnUtc = DateTime.UtcNow;

        Cancel();
    }

    internal void ReverseVendorReturnDelivery()
    {
        if (SourceType != DeliveryNoteSourceType.ToVendorReturn)
            throw new DeliveryNoteCannotChangeStatusException(Status, Status);
        if (Status != DeliveryNoteStatus.Delivered)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Cancelled);

        Status = DeliveryNoteStatus.Cancelled;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    #endregion
}

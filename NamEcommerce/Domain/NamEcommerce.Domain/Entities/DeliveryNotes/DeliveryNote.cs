using NamEcommerce.Domain.Metadata;
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

    private DeliveryNote(Guid id) : base(id)
    {
    }

    internal DeliveryNote(string code, Guid orderId, Guid customerId, decimal amountToCollect, decimal surcharge) : base(Guid.NewGuid())
    {
        Code = code;
        OrderId = orderId;
        CustomerId = customerId;
        Surcharge = surcharge;
        AmountToCollect = amountToCollect;
        Status = DeliveryNoteStatus.Draft;
        CreatedOnUtc = DateTime.UtcNow;
        _items = [];
    }

    public string Code { get; private set; }
    public DeliveryNoteStatus Status { get; private set; }
    public Guid? CreatedByUserId { get; internal set; }
    public bool ShowPrice { get; internal set; }
    public string? Note { get; internal set; }

    public Guid OrderId { get; private set; }
    public string? OrderCode { get; set; }

    public Guid? AssignedDeliveryUserId { get; private set; }
    public string? AssignedDeliveryUsername { get; private set; }
    public string? AssignedDeliveryFullName { get; private set; }
    public DateTime? AssignedDeliveryOnUtc { get; private set; }

    public Guid CustomerId { get; private set; }
    public CustomerInfo CustomerInfo { get; internal set; }
    public NormalizableString ShippingAddress { get; internal set; }
    public string? ShippingPhoneNumber { get; internal set; }

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

    public DateTime? AmountToCollectOverriddenAt { get; private set; }
    public string? AmountToCollectOverrideNote { get; private set; }

    public DeliverySettlementApprovalStatus SettlementApproval { get; private set; } = DeliverySettlementApprovalStatus.NotRequired;
    public decimal? ProposedAmountToCollect { get; private set; }
    public decimal? ApprovedAmountToCollect { get; private set; }
    public decimal? ApprovedAgreedCustomerCharge { get; private set; }
    public string? ApprovedAgreedChargeReason { get; private set; }
    public string? SettlementReason { get; private set; }
    public string? SettlementAdminNote { get; private set; }
    public Guid? SettlementRequestedByUserId { get; private set; }
    public DateTime? SettlementRequestedOnUtc { get; private set; }
    public Guid? SettlementApprovedByUserId { get; private set; }
    public DateTime? SettlementApprovedOnUtc { get; private set; }

    private readonly List<DeliveryNoteSettlementItem> _settlementItems = [];
    public IReadOnlyCollection<DeliveryNoteSettlementItem> SettlementItems => _settlementItems.AsReadOnly();

    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    #region Method

    public bool CanApprove() => !IsDirectShip && Status is DeliveryNoteStatus.Draft;
    public bool CanMarkDelivering() => !IsDirectShip && Status is DeliveryNoteStatus.Confirmed && !AssignedDeliveryUserId.HasValue;
    public bool CanMarkDelivered() => Status is (DeliveryNoteStatus.PendingConfirmation or DeliveryNoteStatus.Delivering);
    public bool CanReject() => IsDirectShip;

    public bool CanEditShippingInfo() => Status is not (DeliveryNoteStatus.Delivered or DeliveryNoteStatus.Cancelled);

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

        RaiseDomainEvent(new DeliveryNoteDelivered(Id, OrderId, CustomerId, AmountToCollect, AmountToCollect));
    }

    internal void MarkCreated() => RaiseDomainEvent(new DeliveryNoteCreated(Id, OrderId, CustomerId));

    internal void UpdateAmountToCollect(decimal amount, string? note)
    {
        if (Status is DeliveryNoteStatus.Delivered or DeliveryNoteStatus.Cancelled)
            throw new NamEcommerceDomainException("Error.DeliveryNote.CannotUpdateAmountWhenCompleted");
        if (amount < 0)
            throw new NamEcommerceDomainException("Error.AmountToCollectCannotBeNegative");

        AmountToCollect = amount;
        AmountToCollectOverriddenAt = DateTime.UtcNow;
        AmountToCollectOverrideNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new DeliveryNoteAmountToCollectUpdated(Id, OrderId, Code, amount));
    }

    internal void UpdateShippingInfo(string shippingAddress, string? shippingPhoneNumber)
    {
        if (Status is DeliveryNoteStatus.Delivered or DeliveryNoteStatus.Cancelled)
            throw new NamEcommerceDomainException("Error.DeliveryNoteCannotUpdateShipping");
        if (string.IsNullOrWhiteSpace(shippingAddress))
            throw new NamEcommerceDomainException("Error.ShippingAddressRequired");
        if (string.IsNullOrWhiteSpace(shippingPhoneNumber))
            throw new NamEcommerceDomainException("Error.PhoneNumberRequired");

        ShippingAddress = shippingAddress.Trim();
        ShippingPhoneNumber = shippingPhoneNumber.Trim();
        UpdatedOnUtc = DateTime.UtcNow;
    }

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
        decimal? cashCollectedAmount = null,
        decimal rejectedGoodsAmount = 0,
        decimal? debtAmount = null)
    {
        if (Status != DeliveryNoteStatus.Delivering
            && Status != DeliveryNoteStatus.Confirmed
            && Status != DeliveryNoteStatus.PendingConfirmation)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Delivered);

        if (SettlementApproval == DeliverySettlementApprovalStatus.PendingApproval)
            throw new NamEcommerceDomainException("Error.DeliverySettlement.NotPending");

        if (pictureIds is null || pictureIds.Count == 0 || pictureIds[0] == Guid.Empty)
            throw new DeliveryProofRequiredException();

        var wasConfirmed = Status == DeliveryNoteStatus.Confirmed;

        Status = DeliveryNoteStatus.Delivered;
        DeliveredOnUtc = DateTime.UtcNow;
        DeliveryProofPictureId = pictureIds[0];
        DeliveryProofPictureIds = pictureIds.ToList().AsReadOnly();
        DeliveryReceiverName = receiverName;
        SetDeliveryCompletionMetadata(latitude, longitude, locationAddress, completionNote, completionSource, idempotencyKey, cashCollectedAmount);
        UpdatedOnUtc = DateTime.UtcNow;

        if (wasConfirmed)
            RaiseDomainEvent(new DeliveryNoteDelivering(Id));
        RaiseDomainEvent(new DeliveryNoteDelivered(
            Id,
            OrderId,
            CustomerId,
            AmountToCollect,
            debtAmount ?? AmountToCollect + rejectedGoodsAmount));
    }

    internal void MarkPendingConfirmation(
        IReadOnlyList<Guid> pictureIds,
        string? receiverName,
        double? latitude = null,
        double? longitude = null,
        string? locationAddress = null,
        string? completionNote = null,
        string? completionSource = null,
        string? idempotencyKey = null,
        decimal? cashCollectedAmount = null,
        IEnumerable<(Guid DeliveryNoteItemId, decimal AcceptedQuantity, decimal RejectedQuantity, string? RejectReason)>? acceptanceLines = null)
    {
        if (Status != DeliveryNoteStatus.Delivering && Status != DeliveryNoteStatus.Confirmed)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.PendingConfirmation);

        if (SettlementApproval == DeliverySettlementApprovalStatus.PendingApproval)
            throw new NamEcommerceDomainException("Error.DeliverySettlement.NotPending");

        if (pictureIds is null || pictureIds.Count == 0 || pictureIds[0] == Guid.Empty)
            throw new DeliveryProofRequiredException();

        var wasConfirmed = Status == DeliveryNoteStatus.Confirmed;

        Status = DeliveryNoteStatus.PendingConfirmation;
        DeliveryProofPictureId = pictureIds[0];
        DeliveryProofPictureIds = pictureIds.ToList().AsReadOnly();
        DeliveryReceiverName = receiverName;
        SetDeliveryCompletionMetadata(latitude, longitude, locationAddress, completionNote, completionSource, idempotencyKey, cashCollectedAmount);
        _settlementItems.Clear();
        if (acceptanceLines is not null)
        {
            foreach (var line in acceptanceLines)
                _settlementItems.Add(new DeliveryNoteSettlementItem(Id, line.DeliveryNoteItemId, line.AcceptedQuantity, line.RejectedQuantity, line.RejectReason));
        }
        UpdatedOnUtc = DateTime.UtcNow;

        if (wasConfirmed)
            RaiseDomainEvent(new DeliveryNoteDelivering(Id));
    }

    internal bool HasSameDeliveryCompletionRequest(string? idempotencyKey)
        => !string.IsNullOrWhiteSpace(idempotencyKey)
           && string.Equals(DeliveryCompletionIdempotencyKey, idempotencyKey.Trim(), StringComparison.OrdinalIgnoreCase);

    internal void UpdateDeliveryCashCollectedAmount(decimal cashCollectedAmount)
    {
        if (Status != DeliveryNoteStatus.Delivered)
            throw new NamEcommerceDomainException("Error.DeliveryNoteMustBeDelivered");
        if (cashCollectedAmount < 0)
            throw new NamEcommerceDomainException("Error.CashCollectedAmountCannotBeNegative");
        if (cashCollectedAmount > AmountToCollect)
            throw new NamEcommerceDomainException("Error.CashCollectedAmountCannotExceedAmountToCollect");

        DeliveryCashCollectedAmount = cashCollectedAmount;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal bool MarkReceivedByCustomer(
        DateTime receivedAtUtc,
        string? receiverName,
        string? note,
        decimal rejectedGoodsAmount = 0,
        decimal? debtAmount = null)
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

        DeliveryConfirmationStatus = DeliveryConfirmationStatus.Confirmed;
        ConfirmedAtUtc = receivedAtUtc;
        ConfirmedNote = note;
        Status = DeliveryNoteStatus.Delivered;
        DeliveredOnUtc = receivedAtUtc;
        DeliveryReceiverName = receiverName;
        UpdatedOnUtc = DateTime.UtcNow;

        if (wasConfirmed)
        {
            RaiseDomainEvent(new DeliveryNoteDelivering(Id));
        }
        var totalAmountToCollect = debtAmount ?? AmountToCollect + rejectedGoodsAmount;
        RaiseDomainEvent(new DeliveryNoteDelivered(Id, OrderId, CustomerId, AmountToCollect,totalAmountToCollect));
        return true;
    }

    internal void RequestSettlementApproval(
        decimal proposedAmountToCollect,
        string? reason,
        IReadOnlyList<Guid> proofPictureIds,
        string? receiverName,
        IEnumerable<(Guid DeliveryNoteItemId, decimal AcceptedQuantity, decimal RejectedQuantity, string? RejectReason)> acceptanceLines,
        Guid? requestedByUserId,
        DateTime requestedOnUtc)
    {
        if (Status != DeliveryNoteStatus.Delivering && Status != DeliveryNoteStatus.Confirmed)
            throw new DeliveryNoteCannotChangeStatusException(Status, Status);
        if (SettlementApproval == DeliverySettlementApprovalStatus.PendingApproval)
            throw new NamEcommerceDomainException("Error.DeliverySettlement.AlreadyPending");
        if (proofPictureIds is null || proofPictureIds.Count == 0 || proofPictureIds[0] == Guid.Empty)
            throw new DeliveryProofRequiredException();

        SettlementApproval = DeliverySettlementApprovalStatus.PendingApproval;
        ProposedAmountToCollect = Math.Max(0m, proposedAmountToCollect);
        SettlementReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ApprovedAmountToCollect = null;
        ApprovedAgreedCustomerCharge = null;
        ApprovedAgreedChargeReason = null;
        SettlementAdminNote = null;
        SettlementApprovedByUserId = null;
        SettlementApprovedOnUtc = null;
        DeliveryProofPictureId = proofPictureIds[0];
        DeliveryProofPictureIds = proofPictureIds.ToList().AsReadOnly();
        DeliveryReceiverName = string.IsNullOrWhiteSpace(receiverName) ? DeliveryReceiverName : receiverName;
        SettlementRequestedByUserId = requestedByUserId;
        SettlementRequestedOnUtc = requestedOnUtc;

        _settlementItems.Clear();
        foreach (var line in acceptanceLines)
            _settlementItems.Add(new DeliveryNoteSettlementItem(Id, line.DeliveryNoteItemId, line.AcceptedQuantity, line.RejectedQuantity, line.RejectReason));

        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new DeliverySettlementApprovalRequested(Id, OrderId, Code));
    }

    internal void ApproveSettlement(
        decimal approvedCashToCollect,
        decimal agreedCustomerCharge,
        string? agreedChargeReason,
        string? adminNote,
        Guid? approvedByUserId,
        DateTime approvedOnUtc)
    {
        if (SettlementApproval != DeliverySettlementApprovalStatus.PendingApproval)
            throw new NamEcommerceDomainException("Error.DeliverySettlement.NotPending");
        if (approvedCashToCollect < 0 || agreedCustomerCharge < 0)
            throw new NamEcommerceDomainException("Error.CashCollectedAmountCannotBeNegative");

        SettlementApproval = DeliverySettlementApprovalStatus.Approved;
        ApprovedAmountToCollect = approvedCashToCollect;
        ApprovedAgreedCustomerCharge = agreedCustomerCharge;
        ApprovedAgreedChargeReason = string.IsNullOrWhiteSpace(agreedChargeReason) ? null : agreedChargeReason.Trim();
        SettlementAdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        SettlementApprovedByUserId = approvedByUserId;
        SettlementApprovedOnUtc = approvedOnUtc;
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new DeliverySettlementApproved(Id, OrderId, Code, approvedCashToCollect));
    }

    internal void RejectSettlement(string reason, Guid? approvedByUserId, DateTime approvedOnUtc)
    {
        if (SettlementApproval != DeliverySettlementApprovalStatus.PendingApproval)
            throw new NamEcommerceDomainException("Error.DeliverySettlement.NotPending");
        if (string.IsNullOrWhiteSpace(reason))
            throw new NamEcommerceDomainException("Error.DeliverySettlement.ReasonRequired");

        SettlementApproval = DeliverySettlementApprovalStatus.Rejected;
        SettlementAdminNote = reason.Trim();
        SettlementApprovedByUserId = approvedByUserId;
        SettlementApprovedOnUtc = approvedOnUtc;
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new DeliverySettlementRejected(Id, OrderId, Code, reason.Trim()));
    }

    internal void Cancel()
    {
        if (Status == DeliveryNoteStatus.Delivered)
            throw new DeliveryNoteCannotChangeStatusException(Status, DeliveryNoteStatus.Cancelled);

        var wasReservingStock = Status == DeliveryNoteStatus.Confirmed
            || Status == DeliveryNoteStatus.Delivering
            || Status == DeliveryNoteStatus.PendingConfirmation;

        Status = DeliveryNoteStatus.Cancelled;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new DeliveryNoteCancelled(Id, wasReservingStock));
    }

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

        RaiseDomainEvent(new DeliveryNoteDelivered(Id, OrderId, CustomerId, AmountToCollect, AmountToCollect));
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

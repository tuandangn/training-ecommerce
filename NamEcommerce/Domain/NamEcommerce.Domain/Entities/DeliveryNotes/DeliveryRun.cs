using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Entities.DeliveryNotes;

[Serializable]
public sealed record DeliveryRun : AppAggregateEntity
{
    public DeliveryRun(Guid id) : base(id)
    {
        Code = string.Empty;
        AssignedDeliveryUsername = string.Empty;
        AssignedDeliveryFullName = string.Empty;
        _items = [];
    }

    internal DeliveryRun(string code, Guid assignedDeliveryUserId, string assignedDeliveryUsername,
        string assignedDeliveryFullName, string? note, Guid? preparedByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        AssignedDeliveryUserId = assignedDeliveryUserId;
        AssignedDeliveryUsername = assignedDeliveryUsername;
        AssignedDeliveryFullName = assignedDeliveryFullName;
        Note = note;
        PreparedByUserId = preparedByUserId;
        PreparedOnUtc = DateTime.UtcNow;
        Status = DeliveryRunStatus.ReadyForHandover;
        CreatedOnUtc = DateTime.UtcNow;
        _items = [];
    }

    public string Code { get; private set; }
    public Guid AssignedDeliveryUserId { get; private set; }
    public string AssignedDeliveryUsername { get; private set; }
    public string AssignedDeliveryFullName { get; private set; }
    public DeliveryRunStatus Status { get; private set; }
    public Guid? PreparedByUserId { get; private set; }
    public DateTime? PreparedOnUtc { get; private set; }
    public Guid? HandedOverByUserId { get; private set; }
    public DateTime? HandedOverOnUtc { get; private set; }
    public DateTime? DriverCachedOnUtc { get; private set; }
    public string? DriverCacheDeviceId { get; private set; }
    public bool PaperManifestIssued { get; private set; }
    public DateTime? PaperManifestIssuedOnUtc { get; private set; }
    public Guid? CashHandoverConfirmedByUserId { get; private set; }
    public string? CashHandoverConfirmedByUsername { get; private set; }
    public string? CashHandoverConfirmedByFullName { get; private set; }
    public DateTime? CashHandoverConfirmedOnUtc { get; private set; }
    public decimal? CashHandoverAmount { get; private set; }
    public string? CashHandoverNote { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    private readonly List<DeliveryRunItem> _items;
    public IReadOnlyCollection<DeliveryRunItem> Items => _items.AsReadOnly();

    internal void AddItem(Guid deliveryNoteId, string deliveryNoteCode, string? orderCode, string customerName,
        string shippingAddress, decimal amountToCollect)
    {
        if (Status != DeliveryRunStatus.ReadyForHandover)
            throw new NamEcommerceDomainException("Error.DeliveryRunCannotChangeItems");
        if (_items.Any(item => item.DeliveryNoteId == deliveryNoteId))
            throw new NamEcommerceDomainException("Error.DeliveryRunDuplicateDeliveryNote");

        _items.Add(new DeliveryRunItem(Id, deliveryNoteId, deliveryNoteCode, orderCode, customerName, shippingAddress, amountToCollect));
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void AcknowledgeDriverCache(string? deviceId, DateTime acknowledgedOnUtc)
    {
        if (Status != DeliveryRunStatus.ReadyForHandover)
            throw new NamEcommerceDomainException("Error.DeliveryRunCannotCache");

        DriverCacheDeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim();
        DriverCachedOnUtc = acknowledgedOnUtc;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void IssuePaperManifest(DateTime issuedOnUtc)
    {
        if (Status != DeliveryRunStatus.ReadyForHandover)
            throw new NamEcommerceDomainException("Error.DeliveryRunCannotIssueManifest");

        PaperManifestIssued = true;
        PaperManifestIssuedOnUtc = issuedOnUtc;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void HandOver(Guid? handedOverByUserId, DateTime handedOverOnUtc)
    {
        if (Status != DeliveryRunStatus.ReadyForHandover)
            throw new NamEcommerceDomainException("Error.DeliveryRunCannotHandOver");
        if (_items.Count == 0)
            throw new NamEcommerceDomainException("Error.DeliveryRunItemsRequired");
        if (!DriverCachedOnUtc.HasValue && !PaperManifestIssued)
            throw new NamEcommerceDomainException("Error.DeliveryRunCacheOrManifestRequired");

        Status = DeliveryRunStatus.HandedToDriver;
        HandedOverByUserId = handedOverByUserId;
        HandedOverOnUtc = handedOverOnUtc;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Close(DateTime closedOnUtc)
    {
        if (Status is not DeliveryRunStatus.HandedToDriver and not DeliveryRunStatus.ReadyForHandover)
            throw new NamEcommerceDomainException("Error.DeliveryRunCannotClose");

        Status = DeliveryRunStatus.Closed;
        UpdatedOnUtc = closedOnUtc;
    }

    internal void ConfirmCashHandover(Guid? cashierUserId, string? cashierUsername, string? cashierFullName,
        decimal amount, string? note, DateTime confirmedOnUtc)
    {
        if (CashHandoverConfirmedOnUtc.HasValue)
            throw new NamEcommerceDomainException("Error.DeliveryRunCashHandoverAlreadyConfirmed");
        if (amount < 0)
            throw new NamEcommerceDomainException("Error.CashHandoverAmountCannotBeNegative");

        CashHandoverConfirmedByUserId = cashierUserId;
        CashHandoverConfirmedByUsername = TrimToNull(cashierUsername);
        CashHandoverConfirmedByFullName = TrimToNull(cashierFullName);
        CashHandoverConfirmedOnUtc = confirmedOnUtc;
        CashHandoverAmount = amount;
        CashHandoverNote = TrimToNull(note);
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Cancel()
    {
        if (Status is DeliveryRunStatus.Closed or DeliveryRunStatus.Cancelled)
            throw new NamEcommerceDomainException("Error.DeliveryRunCannotCancel");

        Status = DeliveryRunStatus.Cancelled;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    private static string? TrimToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.PurchaseOrders;

[Serializable]
public sealed record DirectShipAddressChangeLog : AppAggregateEntity
{
    internal DirectShipAddressChangeLog(
        Guid allocationId,
        string oldAddress, string newAddress,
        string? oldContactName, string? newContactName,
        string? oldContactPhone, string? newContactPhone,
        Guid editedByUserId,
        string? reason)
        : base(Guid.NewGuid())
    {
        if (allocationId == Guid.Empty)
            throw new ArgumentException("AllocationId required.", nameof(allocationId));
        if (string.IsNullOrWhiteSpace(newAddress))
            throw new ArgumentException("NewAddress required.", nameof(newAddress));
        if (editedByUserId == Guid.Empty)
            throw new ArgumentException("EditedByUserId required.", nameof(editedByUserId));

        AllocationId = allocationId;
        OldAddress = oldAddress;
        NewAddress = newAddress;
        OldContactName = oldContactName;
        NewContactName = newContactName;
        OldContactPhone = oldContactPhone;
        NewContactPhone = newContactPhone;
        EditedByUserId = editedByUserId;
        EditedAtUtc = DateTime.UtcNow;
        Reason = reason;
    }

    public Guid AllocationId { get; private set; }
    public string OldAddress { get; private set; }
    public string NewAddress { get; private set; }
    public string? OldContactName { get; private set; }
    public string? NewContactName { get; private set; }
    public string? OldContactPhone { get; private set; }
    public string? NewContactPhone { get; private set; }
    public Guid EditedByUserId { get; private set; }
    public DateTime EditedAtUtc { get; private set; }
    public string? Reason { get; private set; }
}

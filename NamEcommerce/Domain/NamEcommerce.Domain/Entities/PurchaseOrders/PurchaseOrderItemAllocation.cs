using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Entities.PurchaseOrders;

[Serializable]
public sealed record PurchaseOrderItemAllocation : AppAggregateEntity
{
    internal PurchaseOrderItemAllocation(Guid purchaseOrderItemId, Guid orderItemId, decimal allocatedQuantity)
        : base(Guid.NewGuid())
    {
        if (purchaseOrderItemId == Guid.Empty)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemIsNotFound");
        if (orderItemId == Guid.Empty)
            throw new PurchaseOrderItemDataIsInvalidException("Error.OrderItemIsNotFound");
        if (allocatedQuantity <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.AllocatedQuantityMustBePositive");

        PurchaseOrderItemId = purchaseOrderItemId;
        OrderItemId = orderItemId;
        AllocatedQuantity = allocatedQuantity;
        ReceivedQuantity = 0;
        Status = AllocationStatus.Allocated;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid PurchaseOrderItemId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public decimal AllocatedQuantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public AllocationStatus Status { get; private set; }

    public bool IsDirectShip { get; private set; }
    public string? DirectShipAddress { get; private set; }
    public string? DirectShipContactName { get; private set; }
    public string? DirectShipContactPhone { get; private set; }
    /// <summary>Số càng cao càng ưu tiên khi NCC giao thiếu.</summary>
    public int DirectShipPriority { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    #region Methods

    internal void IncreaseReceived(decimal receivedQuantity)
    {
        if (receivedQuantity <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderReceiveQuantityMustBePositive");
        if (ReceivedQuantity + receivedQuantity > AllocatedQuantity)
            throw new PurchaseOrderItemDataIsInvalidException("Error.ReceivedQuantityCannotExceedAllocatedQuantity");

        ReceivedQuantity += receivedQuantity;
        Status = ReceivedQuantity >= AllocatedQuantity ? AllocationStatus.FullyReceived : AllocationStatus.PartiallyReceived;
    }

    internal void ReduceReceived(decimal receivedQuantity)
    {
        if (receivedQuantity <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderReceiveQuantityMustBePositive");
        if (receivedQuantity > ReceivedQuantity)
            throw new PurchaseOrderItemDataIsInvalidException("Error.ReceivedQuantityCannotBeNegative");

        ReceivedQuantity -= receivedQuantity;
        Status = ReceivedQuantity <= 0
            ? AllocationStatus.Allocated
            : ReceivedQuantity >= AllocatedQuantity
                ? AllocationStatus.FullyReceived
                : AllocationStatus.PartiallyReceived;
    }

    internal void ReduceAllocationToReceived()
    {
        AllocatedQuantity = ReceivedQuantity;
        Status = ReceivedQuantity <= 0 ? AllocationStatus.Cancelled : AllocationStatus.FullyReceived;
    }

    internal void SetDirectShip(string address, string? contactName, string? contactPhone, int priority)
    {
        if (string.IsNullOrWhiteSpace(contactPhone))
            throw new PurchaseOrderItemDataIsInvalidException("Error.DirectShipContactPhoneRequired");
        if (string.IsNullOrWhiteSpace(address))
            throw new PurchaseOrderItemDataIsInvalidException("Error.DirectShipAddressRequired");

        IsDirectShip = true;
        DirectShipAddress = address.Trim();
        DirectShipContactName = string.IsNullOrWhiteSpace(contactName) ? null : contactName.Trim();
        DirectShipContactPhone = contactPhone.Trim();
        DirectShipPriority = priority;
        RaiseDomainEvent(new AllocationMarkedAsDirectShip(Id, PurchaseOrderItemId, DirectShipAddress, DirectShipContactName, DirectShipContactPhone));
    }

    internal void ClearDirectShip()
    {
        IsDirectShip = false;
        DirectShipAddress = null;
        DirectShipContactName = null;
        DirectShipContactPhone = null;
        DirectShipPriority = 0;
    }

    internal void UpdateDirectShipInfo(string address, string? contactName, string? contactPhone, Guid editedByUserId)
    {
        if (string.IsNullOrWhiteSpace(contactPhone))
            throw new PurchaseOrderItemDataIsInvalidException("Error.DirectShipContactPhoneRequired");
        if (string.IsNullOrWhiteSpace(address))
            throw new PurchaseOrderItemDataIsInvalidException("Error.DirectShipAddressRequired");

        var oldAddress = DirectShipAddress ?? string.Empty;
        DirectShipAddress = address.Trim();
        DirectShipContactName = string.IsNullOrWhiteSpace(contactName) ? null : contactName.Trim();
        DirectShipContactPhone = contactPhone.Trim();
        RaiseDomainEvent(new DirectShipAddressUpdated(Id, oldAddress, DirectShipAddress, editedByUserId));
    }

    internal void SetStatus(AllocationStatus newStatus) => Status = newStatus;

    #endregion
}

using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerReturnRequest : AppAggregateEntity
{
    private readonly List<CustomerReturnRequestItem> _items = [];

    private CustomerReturnRequest() : base(Guid.NewGuid()) { }

    internal CustomerReturnRequest(Guid customerId, Guid deliveryNoteId, string? reason) : base(Guid.NewGuid())
    {
        CustomerId = customerId;
        DeliveryNoteId = deliveryNoteId;
        Reason = reason;
        Status = CustomerReturnRequestStatus.PendingReview;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }
    public Guid DeliveryNoteId { get; private set; }
    public CustomerReturnRequestStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public string? AdminNote { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ReviewedOnUtc { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public Guid? ConvertedCustomerReturnId { get; private set; }
    public IReadOnlyCollection<CustomerReturnRequestItem> Items => _items.AsReadOnly();

    internal void AddItem(Guid deliveryNoteItemId, Guid productId, string productName, decimal requestedQuantity, string? reason)
    {
        if (requestedQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedQuantity));

        _items.Add(new CustomerReturnRequestItem(Id, deliveryNoteItemId, productId, productName, requestedQuantity, reason));
    }

    internal void Accept(Guid reviewedByUserId, string? adminNote, DateTime nowUtc)
    {
        EnsurePending();
        Status = CustomerReturnRequestStatus.Accepted;
        ReviewedByUserId = reviewedByUserId;
        AdminNote = adminNote;
        ReviewedOnUtc = nowUtc;
    }

    internal void Reject(Guid reviewedByUserId, string? adminNote, DateTime nowUtc)
    {
        EnsurePending();
        Status = CustomerReturnRequestStatus.Rejected;
        ReviewedByUserId = reviewedByUserId;
        AdminNote = adminNote;
        ReviewedOnUtc = nowUtc;
    }

    internal void Cancel(DateTime nowUtc)
    {
        EnsurePending();
        Status = CustomerReturnRequestStatus.Cancelled;
        ReviewedOnUtc = nowUtc;
    }

    internal void MarkConverted(Guid customerReturnId, DateTime nowUtc)
    {
        if (Status != CustomerReturnRequestStatus.Accepted)
            throw new InvalidOperationException("Only accepted return requests can be converted.");

        Status = CustomerReturnRequestStatus.ConvertedToReturn;
        ConvertedCustomerReturnId = customerReturnId;
        ReviewedOnUtc = nowUtc;
    }

    private void EnsurePending()
    {
        if (Status != CustomerReturnRequestStatus.PendingReview)
            throw new InvalidOperationException("Return request is not pending review.");
    }
}

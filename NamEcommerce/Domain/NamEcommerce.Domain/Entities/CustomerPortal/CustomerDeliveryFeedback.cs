using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerDeliveryFeedback : AppAggregateEntity
{
    private CustomerDeliveryFeedback() : base(Guid.NewGuid()) { }

    internal CustomerDeliveryFeedback(Guid customerId, Guid deliveryNoteId, int? rating, string? message) : base(Guid.NewGuid())
    {
        CustomerId = customerId;
        DeliveryNoteId = deliveryNoteId;
        Rating = rating;
        Message = message;
        Status = CustomerDeliveryFeedbackStatus.New;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }
    public Guid DeliveryNoteId { get; private set; }
    public int? Rating { get; private set; }
    public string? Message { get; private set; }
    public CustomerDeliveryFeedbackStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ReviewedOnUtc { get; private set; }

    internal void MarkReviewed(DateTime nowUtc)
    {
        Status = CustomerDeliveryFeedbackStatus.Reviewed;
        ReviewedOnUtc = nowUtc;
    }
}

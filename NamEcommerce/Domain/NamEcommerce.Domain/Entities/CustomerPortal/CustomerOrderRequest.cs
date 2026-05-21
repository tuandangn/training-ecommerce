using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerOrderRequest : AppAggregateEntity
{
    private readonly List<CustomerOrderRequestItem> _items = [];

    private CustomerOrderRequest() : base(Guid.NewGuid()) { }

    internal CustomerOrderRequest(Guid customerId, string code) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        CustomerId = customerId;
        Code = code;
        Status = CustomerOrderRequestStatus.PendingApproval;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public CustomerOrderRequestStatus Status { get; private set; }
    public DateTime? ExpectedShippingDateUtc { get; internal set; }
    public string? ShippingAddress { get; internal set; }
    public string? Note { get; internal set; }
    public string? AdminNote { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ReviewedOnUtc { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public Guid? ConvertedOrderId { get; private set; }
    public IReadOnlyCollection<CustomerOrderRequestItem> Items => _items.AsReadOnly();

    internal void AddItem(Guid productId, string productName, decimal quantity, decimal unitPriceSnapshot)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPriceSnapshot < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPriceSnapshot));

        _items.Add(new CustomerOrderRequestItem(Id, productId, productName, quantity, unitPriceSnapshot));
    }

    internal void Approve(Guid reviewedByUserId, string? adminNote, DateTime nowUtc)
    {
        EnsurePending();
        if (_items.Count == 0 || _items.Any(item => item.UnitPriceSnapshot <= 0))
            throw new InvalidOperationException("Order request contains unpriced items.");

        Status = CustomerOrderRequestStatus.Approved;
        ReviewedByUserId = reviewedByUserId;
        AdminNote = adminNote;
        ReviewedOnUtc = nowUtc;
    }

    internal void Reject(Guid reviewedByUserId, string? adminNote, DateTime nowUtc)
    {
        EnsurePending();
        Status = CustomerOrderRequestStatus.Rejected;
        ReviewedByUserId = reviewedByUserId;
        AdminNote = adminNote;
        ReviewedOnUtc = nowUtc;
    }

    internal void Cancel(DateTime nowUtc)
    {
        EnsurePending();
        Status = CustomerOrderRequestStatus.Cancelled;
        ReviewedOnUtc = nowUtc;
    }

    internal void MarkConverted(Guid orderId, DateTime nowUtc)
    {
        if (Status != CustomerOrderRequestStatus.Approved)
            throw new InvalidOperationException("Only approved order requests can be converted.");

        Status = CustomerOrderRequestStatus.ConvertedToOrder;
        ConvertedOrderId = orderId;
        ReviewedOnUtc = nowUtc;
    }

    internal void UpdateItemPrices(IReadOnlyDictionary<Guid, decimal> itemPrices)
    {
        EnsurePending();
        if (itemPrices.Count != _items.Count)
            throw new InvalidOperationException("Order request pricing is incomplete.");

        foreach (var item in _items)
        {
            if (!itemPrices.TryGetValue(item.Id, out var unitPrice))
                throw new InvalidOperationException("Order request pricing is incomplete.");

            item.UpdateUnitPriceSnapshot(unitPrice);
        }
    }

    private void EnsurePending()
    {
        if (Status != CustomerOrderRequestStatus.PendingApproval)
            throw new InvalidOperationException("Order request is not pending approval.");
    }
}

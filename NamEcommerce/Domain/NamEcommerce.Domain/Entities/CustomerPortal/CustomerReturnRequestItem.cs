using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerReturnRequestItem : AppAggregateEntity
{
    private readonly List<CustomerReturnRequestItemPicture> _evidencePictures = [];

    private CustomerReturnRequestItem() : base(Guid.NewGuid()) { }

    internal CustomerReturnRequestItem(
        Guid customerReturnRequestId,
        Guid deliveryNoteItemId,
        Guid productId,
        string productName,
        decimal requestedQuantity,
        string? reason) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        CustomerReturnRequestId = customerReturnRequestId;
        DeliveryNoteItemId = deliveryNoteItemId;
        ProductId = productId;
        ProductName = productName;
        RequestedQuantity = requestedQuantity;
        Reason = reason;
    }

    public Guid CustomerReturnRequestId { get; private set; }
    public Guid DeliveryNoteItemId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal RequestedQuantity { get; private set; }
    public string? Reason { get; private set; }
    public IReadOnlyCollection<CustomerReturnRequestItemPicture> EvidencePictures => _evidencePictures.AsReadOnly();

    internal void AddEvidencePicture(Guid pictureId)
        => _evidencePictures.Add(new CustomerReturnRequestItemPicture(Id, pictureId));
}

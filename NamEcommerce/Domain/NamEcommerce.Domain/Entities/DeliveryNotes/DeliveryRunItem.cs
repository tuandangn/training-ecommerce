using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.DeliveryNotes;

[Serializable]
public sealed record DeliveryRunItem : AppEntity
{
    public DeliveryRunItem(Guid id) : base(id)
    {
        DeliveryNoteCode = string.Empty;
        CustomerName = string.Empty;
        ShippingPhoneNumber = string.Empty;
        ShippingAddress = string.Empty;
    }

    internal DeliveryRunItem(Guid deliveryRunId, Guid deliveryNoteId, string deliveryNoteCode, string? orderCode,
        string customerName, string? shippingPhoneNumber, string shippingAddress, decimal amountToCollect) : base(Guid.NewGuid())
    {
        DeliveryRunId = deliveryRunId;
        DeliveryNoteId = deliveryNoteId;
        DeliveryNoteCode = deliveryNoteCode;
        OrderCode = orderCode;
        CustomerName = customerName;
        ShippingPhoneNumber = shippingPhoneNumber;
        ShippingAddress = shippingAddress;
        AmountToCollect = amountToCollect;
    }

    public Guid DeliveryRunId { get; private set; }
    public Guid DeliveryNoteId { get; private set; }
    public string DeliveryNoteCode { get; private set; }
    public string? OrderCode { get; private set; }
    public string CustomerName { get; private set; }
    public string? ShippingPhoneNumber { get; private set; }
    public string ShippingAddress { get; private set; }
    public decimal AmountToCollect { get; private set; }
}

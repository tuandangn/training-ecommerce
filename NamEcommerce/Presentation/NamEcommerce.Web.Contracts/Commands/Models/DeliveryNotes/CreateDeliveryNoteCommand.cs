using MediatR;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

[Serializable]
public sealed class CreateDeliveryNoteCommand : IRequest<CreateDeliveryNoteResultModel>
{
    public Guid OrderId { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public bool ShowPrice { get; set; }
    public bool CompensateReturnedQuantityInNextDelivery { get; set; }
    public string? Note { get; set; }
    public decimal Surcharge { get; set; }
    public string? SurchargeReason { get; set; }
    public decimal AmountToCollect { get; set; }

    public IList<CreateDeliveryNoteItemModel> Items { get; set; } = [];

    [Serializable]
    public sealed class CreateDeliveryNoteItemModel
    {
        public Guid OrderItemId { get; set; }
        public Guid WarehouseId { get; set; }
        public decimal Quantity { get; set; }
    }
}

[Serializable]
public sealed class CreateDeliveryNoteResultModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public Guid? CreatedId { get; set; }

    public void Deconstruct(out bool success, out string? errorMessage)
        => (success, errorMessage) = (Success, ErrorMessage);
}

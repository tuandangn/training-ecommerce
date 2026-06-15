
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

[Serializable]
public sealed class CreateDeliveryNoteCommand : ICommand<CreateDeliveryNoteResultModel>
{
    public Guid OrderId { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string? ShippingPhoneNumber { get; set; }
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
        public int QuantityDecimalPlaces { get; set; }
    }
}

[Serializable]
public sealed record UpdateDeliveryNoteShippingCommand(Guid DeliveryNoteId, string? ShippingAddress, string? ShippingPhoneNumber)
    : ICommand<CommonActionResultModel>;

[Serializable]
public sealed class CreateDeliveryNoteResultModel : ICommandResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public Guid? CreatedId { get; set; }

    public void Deconstruct(out bool success, out string? errorMessage)
        => (success, errorMessage) = (Success, ErrorMessage);
}

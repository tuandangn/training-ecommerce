using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace NamEcommerce.Web.Models.DeliveryNotes;

[Serializable]
public sealed class MarkDeliveryNoteAsDeliveredModel
{
    public Guid DeliveryNoteId { get; set; }

    public string? ReceiverName { get; set; }
    public decimal? CashCollectedAmount { get; set; }
    public bool CompensateInNextDelivery { get; set; }

    public decimal AgreedCustomerCharge { get; set; }
    public string? AgreedCustomerChargeReason { get; set; }

    public IList<Guid> PictureIds { get; set; } = [];

    [ValidateNever]
    public string? AcceptanceItemsJson { get; set; }
}

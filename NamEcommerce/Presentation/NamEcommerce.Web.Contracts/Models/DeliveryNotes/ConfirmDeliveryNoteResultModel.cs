using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Models.DeliveryNotes;

[Serializable]
public sealed class ConfirmDeliveryNoteResultModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public IList<ShortageInfoItemModel> ShortageItems { get; set; } = [];
}


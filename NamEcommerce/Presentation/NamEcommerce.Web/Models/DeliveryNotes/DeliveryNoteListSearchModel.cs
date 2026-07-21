using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.DeliveryNotes;

[Serializable]
public sealed record DeliveryNoteListSearchModel : BasePaginationModel
{
    public string? Keywords { get; set; }
}

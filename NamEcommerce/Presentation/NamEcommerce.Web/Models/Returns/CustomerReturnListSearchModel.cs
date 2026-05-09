using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.Returns;

[Serializable]
public sealed record CustomerReturnListSearchModel : BasePaginationModel
{
    public Guid? CustomerId { get; set; }
    public Guid? DeliveryNoteId { get; set; }
    public int? Status { get; set; }
}

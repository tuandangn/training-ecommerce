using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed record OrderListSearchModel : BasePaginationModel
{
    public string? Keywords { get; set; }
    public int? Status { get; set; }
}

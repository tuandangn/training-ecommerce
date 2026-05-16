using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed record PurchaseOrderListSearchModel : BasePaginationModel
{
    public string? Keywords { get; set; }
    public PurchaseOrderStatus? Status { get; set; }
}

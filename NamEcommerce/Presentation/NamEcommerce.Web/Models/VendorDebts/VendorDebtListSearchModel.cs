using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.VendorDebts;

[Serializable]
public sealed record VendorDebtListSearchModel : BasePaginationModel
{
    public string? Keywords { get; set; }
}

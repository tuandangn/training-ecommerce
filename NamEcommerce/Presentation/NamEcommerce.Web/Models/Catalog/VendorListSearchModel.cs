using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.Catalog;

[Serializable]
public sealed record VendorListSearchModel : BasePaginationModel
{
    public string? Keywords { get; set; }
}

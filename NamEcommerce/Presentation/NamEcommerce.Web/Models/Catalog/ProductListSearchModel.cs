using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.Catalog;

[Serializable]
public sealed record ProductListSearchModel : BasePaginationModel
{
    public string? Keywords { get; set; }
}

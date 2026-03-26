using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.Catalog;

[Serializable]
public sealed record CategoryListSearchModel : BasePaginationModel
{
    public string? Keywords { get; set; }
}

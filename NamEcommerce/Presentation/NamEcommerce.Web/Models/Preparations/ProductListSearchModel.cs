using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.Preparations;

[Serializable]
public sealed record PreparationListSearchModel : BasePaginationModel
{
    public string? Keywords { get; set; }
}

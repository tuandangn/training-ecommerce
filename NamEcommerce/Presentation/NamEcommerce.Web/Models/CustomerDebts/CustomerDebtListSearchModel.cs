using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.CustomerDebts;

[Serializable]
public sealed record CustomerDebtListSearchModel : BasePaginationModel
{
    public string? Keywords { get; set; }
}

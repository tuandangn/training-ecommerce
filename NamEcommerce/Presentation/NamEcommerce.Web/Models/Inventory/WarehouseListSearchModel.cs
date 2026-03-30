using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.Inventory;

[Serializable]
public sealed record WarehouseListSearchModel : BasePaginationModel
{
    public string? Keywords { get; set; }
}

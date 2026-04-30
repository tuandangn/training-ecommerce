using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Models.Catalog;

[Serializable]
public sealed record ProductSearchModel : BasePaginationModel
{
    public string? Q { get; set; }
    public Guid? Vid { get; set; }
    public Guid? Cid { get; set; }
}

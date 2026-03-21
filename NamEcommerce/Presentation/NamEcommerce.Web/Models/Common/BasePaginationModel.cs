namespace NamEcommerce.Web.Models.Common;

[Serializable]
public record BasePaginationModel
{
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}

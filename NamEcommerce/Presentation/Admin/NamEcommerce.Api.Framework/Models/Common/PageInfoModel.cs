namespace NamEcommerce.Admin.Contracts.Models.Common;

[Serializable]
public sealed record PageInfoModel(int TotalCount, int PageIndex, int PageSize)
{
    public int PageNumber => PageIndex + 1;
}

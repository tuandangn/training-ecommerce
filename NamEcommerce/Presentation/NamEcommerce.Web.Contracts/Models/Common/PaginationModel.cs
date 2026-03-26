namespace NamEcommerce.Web.Contracts.Models.Common;

[Serializable]
public sealed record PaginationModel
{
    public required int PageIndex
    {
        get;
        set => field = value >= 0 ? value : throw new ArgumentException("PageIndex must greater than or equal 0");
    }
    public int PageNumber => PageIndex + 1;
    public required int PageSize
    {
        get;
        set => field = value > 0 ? value : throw new ArgumentException("PageSize must greater than 0");
    }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public required int TotalCount
    {
        get;
        set => field = value >= 0 ? value : throw new ArgumentException("TotalCount must greater than or equal 0");
    }
}

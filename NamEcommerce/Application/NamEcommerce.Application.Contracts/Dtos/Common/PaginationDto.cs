namespace NamEcommerce.Application.Contracts.Dtos.Common;

[Serializable]
public sealed record PaginationDto
{
    public required int PageIndex
    {
        get;
        set => field = value >= 0 ? value : throw new ArgumentException("PageIndex phải lớn hơn hoặc bằng 0");
    }
    public int PageNumber => PageIndex + 1;
    public required int PageSize
    {
        get;
        set => field = value > 0 ? value : throw new ArgumentException("PageSize phải lớn hơn 0");
    }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public required int TotalCount
    {
        get;
        set => field = value >= 0 ? value : throw new ArgumentException("TotalCount phải lớn hơn hoặc bằng 0");
    }
}

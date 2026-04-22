namespace NamEcommerce.Application.Contracts.Dtos.Common;

[Serializable]
public sealed record PaginationDto
{
    public required int PageIndex
    {
        get;
        set => field = value >= 0 ? value : throw new ArgumentException("Error.PaginationPageIndexInvalid");
    }
    public int PageNumber => PageIndex + 1;
    public required int PageSize
    {
        get;
        set => field = value > 0 ? value : throw new ArgumentException("Error.PaginationPageSizeInvalid");
    }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public required int TotalCount
    {
        get;
        set => field = value >= 0 ? value : throw new ArgumentException("Error.PaginationTotalCountInvalid");
    }
}

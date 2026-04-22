using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.Common;

[Serializable]
public sealed record PagerInfoDto
{
    public required int PageIndex
    {
        get;
        set
        {
            field = value >= 0
                ? value
                : throw new NamEcommerceDomainException("Error.PageIndexInvalid");
        }
    }
    public int PageNumber => PageIndex + 1;
    public required int PageSize
    {
        get;
        set
        {
            field = value > 0
                ? value
                : throw new NamEcommerceDomainException("Error.PageSizeInvalid");
        }
    }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public required int TotalCount
    {
        get;
        set
        {
            field = value >= 0
                ? value
                : throw new NamEcommerceDomainException("Error.TotalCountInvalid");
        }
    }
}

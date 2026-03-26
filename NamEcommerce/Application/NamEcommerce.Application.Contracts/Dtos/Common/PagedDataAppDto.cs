using System.Collections;

namespace NamEcommerce.Application.Contracts.Dtos.Common;

public interface IPagedDataAppDto<TData> : IEnumerable, IEnumerable<TData>
{
    IEnumerable<TData> Items { get; set; }
    PaginationDto Pagination { get; set; }
}

[Serializable]
public sealed class PagedDataAppDto<TData> : IPagedDataAppDto<TData>, IEnumerable, IEnumerable<TData>
{
    public required IEnumerable<TData> Items { get; set; }
    public required PaginationDto Pagination { get; set; }

    public IEnumerator<TData> GetEnumerator()
        => Items?.GetEnumerator() ?? throw new InvalidOperationException();
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}

public static class PagedDataAppDto
{
    public static IPagedDataAppDto<TData> Create<TData>(IEnumerable<TData> items, int? pageIndex = null, int? pageSize = null)
        => new PagedDataAppDto<TData>()
        {
            Items = items,
            Pagination = new PaginationDto
            {
                PageIndex = pageIndex ?? 0,
                PageSize = pageSize ?? items.Count(),
                TotalCount = items.Count()
            }
        };

    public static IPagedDataAppDto<TData> Create<TData>(IEnumerable<TData> items, int pageIndex, int pageSize, int totalCount)
        => new PagedDataAppDto<TData>()
        {
            Items = items,
            Pagination = new PaginationDto
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        };
}

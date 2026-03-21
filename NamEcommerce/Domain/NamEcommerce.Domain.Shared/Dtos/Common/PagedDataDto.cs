using System.Collections;

namespace NamEcommerce.Domain.Shared.Dtos.Common;

public interface IPagedDataDto<TData> : IEnumerable, IEnumerable<TData>
{
    IEnumerable<TData> Items { get; set; }
    PagerInfoDto PagerInfo { get; set; }
}

[Serializable]
public sealed class PagedDataDto<TData> : IPagedDataDto<TData>, IEnumerable, IEnumerable<TData>
{
    public required IEnumerable<TData> Items { get; set; }
    public required PagerInfoDto PagerInfo { get; set; }

    public IEnumerator<TData> GetEnumerator()
        => Items?.GetEnumerator() ?? throw new InvalidOperationException();
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}

public static class PagedDataDto
{
    public static IPagedDataDto<TData> Create<TData>(IEnumerable<TData> items, int? pageIndex = null, int? pageSize = null)
        => new PagedDataDto<TData>()
        {
            Items = items,
            PagerInfo = new PagerInfoDto
            {
                PageIndex = pageIndex ?? 0,
                PageSize = pageSize ?? items.Count(),
                TotalCount = items.Count()
            }
        };

    public static IPagedDataDto<TData> Create<TData>(IEnumerable<TData> items, int pageIndex, int pageSize, int totalCount)
        => new PagedDataDto<TData>()
        {
            Items = items,
            PagerInfo = new PagerInfoDto
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        };
}

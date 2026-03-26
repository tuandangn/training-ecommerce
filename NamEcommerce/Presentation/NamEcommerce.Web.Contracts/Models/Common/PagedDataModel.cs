using System.Collections;

namespace NamEcommerce.Web.Contracts.Models.Common;

public interface IPagedDataModel<TData> : IEnumerable, IEnumerable<TData>
{
    IEnumerable<TData> Items { get; set; }
    PaginationModel Pagination { get; set; }
}

[Serializable]
public sealed class PagedDataModel<TData> : IPagedDataModel<TData>, IEnumerable, IEnumerable<TData>
{
    public required IEnumerable<TData> Items { get; set; }
    public required PaginationModel Pagination { get; set; }

    public IEnumerator<TData> GetEnumerator()
        => Items?.GetEnumerator() ?? throw new InvalidOperationException();
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}

public static class PagedDataModel
{
    public static IPagedDataModel<TData> Create<TData>(IEnumerable<TData> items, int? pageIndex = null, int? pageSize = null)
        => new PagedDataModel<TData>()
        {
            Items = items,
            Pagination = new PaginationModel
            {
                PageIndex = pageIndex ?? 0,
                PageSize = pageSize ?? items.Count(),
                TotalCount = items.Count()
            }
        };

    public static IPagedDataModel<TData> Create<TData>(IEnumerable<TData> items, int pageIndex, int pageSize, int totalCount)
        => new PagedDataModel<TData>()
        {
            Items = items,
            Pagination = new PaginationModel
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        };
}

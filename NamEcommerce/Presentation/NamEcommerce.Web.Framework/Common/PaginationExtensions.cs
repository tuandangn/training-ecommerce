using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Web.Contracts.Common;

namespace NamEcommerce.Web.Framework.Common;

public static class PaginationExtensions
{
    public static IPagedDataModel<TTarget> MapToModel<TSource, TTarget>(
        this IPagedDataDto<TSource> pagedDto, Func<TSource, TTarget> converter)
        => PagedDataModel.Create(
            pagedDto.Items.Select(converter), 
            pagedDto.Pagination.PageIndex,
            pagedDto.Pagination.PageSize,
            pagedDto.Pagination.TotalCount);
}

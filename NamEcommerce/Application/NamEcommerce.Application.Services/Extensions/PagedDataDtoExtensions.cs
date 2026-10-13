using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;

namespace NamEcommerce.Application.Services.Extensions;

public static class PagedDataDtoExtensions
{
    public static IPagedDataAppDto<TAppDto> ToPagedDataAppDto<TDto, TAppDto>(this IPagedDataDto<TDto> pagedData, Func<TDto, TAppDto> mapFunc)
    {
        if (pagedData is null)
            throw new ArgumentNullException(nameof(pagedData));
        if (mapFunc is null)
            throw new ArgumentNullException(nameof(mapFunc));

        var (pageIndex, pageSize, totalCount) = pagedData.PagerInfo;
        return PagedDataAppDto.Create(pagedData.Select(mapFunc).ToList(), pageIndex, pageSize, totalCount);
    }
}

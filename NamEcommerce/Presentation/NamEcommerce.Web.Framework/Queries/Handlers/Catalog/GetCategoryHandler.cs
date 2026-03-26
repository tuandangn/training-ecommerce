using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetCategoryHandler : IRequestHandler<GetCategoryQuery, CategoryModel?>
{
    private readonly ICategoryAppService _categoryAppService;

    public GetCategoryHandler(ICategoryAppService categoryAppService)
    {
        _categoryAppService = categoryAppService;
    }

    public async Task<CategoryModel?> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
    {
        var category = await _categoryAppService.GetCategoryByIdAsync(request.Id);
        if (category == null)
            return null;

        return new CategoryModel
        {
            Id = category.Id,
            Name = category.Name,
            DisplayOrder = category.DisplayOrder,
            ParentId = category.ParentId
        };
    }
}

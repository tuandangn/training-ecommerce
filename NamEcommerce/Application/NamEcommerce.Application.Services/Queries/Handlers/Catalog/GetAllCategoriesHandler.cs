using MediatR;
using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Queries.Handlers.Catalog;

public sealed class GetAllCategoriesHandler : IRequestHandler<GetAllCategories, IEnumerable<CategoryDto>>
{
    private readonly ICategoryManager _categoryManager;

    public GetAllCategoriesHandler(ICategoryManager categoryManager)
    {
        _categoryManager = categoryManager;
    }

    public async Task<IEnumerable<CategoryDto>> Handle(GetAllCategories request, CancellationToken cancellationToken)
    {
        var categories = await _categoryManager.GetAllCategoriesAsync().ConfigureAwait(false);
        return categories.OrderBy(category => category.DisplayOrder)
            .Select(category => new CategoryDto(category.Id, category.Name)
            {
                DisplayOrder = category.DisplayOrder
            });
    }
}

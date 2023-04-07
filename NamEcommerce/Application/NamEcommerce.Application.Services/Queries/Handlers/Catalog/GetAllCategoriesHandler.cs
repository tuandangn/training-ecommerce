using MediatR;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Queries.Handlers.Catalog;

public sealed class GetAllCategoriesHandler : IRequestHandler<GetAllCategories, IEnumerable<CategoryDto>>
{
    private readonly IEntityDataReader<Category> _categoryDataReader;

    public GetAllCategoriesHandler(IEntityDataReader<Category> categoryDataReader)
    {
        _categoryDataReader = categoryDataReader;
    }

    public async Task<IEnumerable<CategoryDto>> Handle(GetAllCategories request, CancellationToken cancellationToken)
    {
        var categories = await _categoryDataReader.GetAllAsync().ConfigureAwait(false);
        return categories.OrderBy(category => category.DisplayOrder)
            .Select(category => category.ToDto());
    }
}

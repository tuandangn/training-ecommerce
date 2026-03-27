using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Queries.Catalog;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Application.Services.Queries.Handlers.Catalog;

public sealed class GetAllCategoriesHandler : IRequestHandler<GetAllCategories, IEnumerable<CategoryAppDto>>
{
    private readonly IEntityDataReader<Category> _categoryDataReader;

    public GetAllCategoriesHandler(IEntityDataReader<Category> categoryDataReader)
    {
        _categoryDataReader = categoryDataReader;
    }

    public async Task<IEnumerable<CategoryAppDto>> Handle(GetAllCategories request, CancellationToken cancellationToken)
    {
        var categories = await _categoryDataReader.GetAllAsync().ConfigureAwait(false);
        return categories.OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => category.ToDto());
    }
}

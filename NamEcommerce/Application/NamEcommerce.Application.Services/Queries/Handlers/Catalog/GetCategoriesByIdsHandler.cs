using MediatR;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Queries.Handlers.Catalog;

public sealed class GetCategoriesByIdsHandler : IRequestHandler<GetCategoriesByIds, IEnumerable<CategoryDto>>
{
    private readonly IEntityDataReader<Category> _categoryDataReader;

    public GetCategoriesByIdsHandler(IEntityDataReader<Category> categoryDataReader)
    {
        _categoryDataReader = categoryDataReader;
    }

    public async Task<IEnumerable<CategoryDto>> Handle(GetCategoriesByIds request, CancellationToken cancellationToken)
    {
        if (request.Ids.Count() == 0)
            return Enumerable.Empty<CategoryDto>();

        var categories = await _categoryDataReader.GetByIdsAsync(request.Ids);

        return categories.Select(category => category.ToDto());
    }
}

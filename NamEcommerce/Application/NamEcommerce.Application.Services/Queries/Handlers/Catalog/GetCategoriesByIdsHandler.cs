using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Queries.Catalog;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Queries.Handlers.Catalog;

public sealed class GetCategoriesByIdsHandler : IRequestHandler<GetCategoriesByIds, IEnumerable<CategoryAppDto>>
{
    private readonly IEntityDataReader<Category> _categoryDataReader;

    public GetCategoriesByIdsHandler(IEntityDataReader<Category> categoryDataReader)
    {
        _categoryDataReader = categoryDataReader;
    }

    public async Task<IEnumerable<CategoryAppDto>> Handle(GetCategoriesByIds request, CancellationToken cancellationToken)
    {
        if (request.Ids.Count() == 0)
            return Enumerable.Empty<CategoryAppDto>();

        var categories = await _categoryDataReader.GetByIdsAsync(request.Ids);

        return categories.Select(category => category.ToDto());
    }
}

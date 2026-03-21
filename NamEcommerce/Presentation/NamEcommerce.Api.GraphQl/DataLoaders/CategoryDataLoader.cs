using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Queries.Catalog;

namespace NamEcommerce.Api.GraphQl.DataLoaders;

public sealed class CategoryDataLoader : ICategoryDataLoader
{
    public const string GET_ALL = "CategoryDataLoader.GetAll";
    public const string GET_BY_ID = "CategoryDataLoader.GetById";

    private readonly IMediator _mediator;

    public CategoryDataLoader(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<IEnumerable<CategoryAppDto>> GetAllCategoriesAsync(CancellationToken cancellationToken)
        => _mediator.Send(new GetAllCategories(), cancellationToken);

    public async Task<CategoryAppDto?> GetCategoryByIdAsync(CancellationToken cancellationToken, Guid? id)
    {
        if (!id.HasValue)
            return null;
        return await _mediator.Send(new GetCategoryById(id.Value), cancellationToken);
    }

    public async Task<IDictionary<Guid, CategoryAppDto>> GetCategoriesByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var categories = await _mediator.Send(new GetCategoriesByIds(ids), cancellationToken);

        return categories.ToDictionary(category => category.Id);
    }
}

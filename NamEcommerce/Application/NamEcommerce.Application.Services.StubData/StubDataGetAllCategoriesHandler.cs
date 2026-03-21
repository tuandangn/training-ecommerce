using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Queries.Catalog;

namespace NamEcommerce.Application.Services.StubData;

public sealed class StubDataGetAllCategoriesHandler : IRequestHandler<GetAllCategories, IEnumerable<CategoryAppDto>>
{
    public Task<IEnumerable<CategoryAppDto>> Handle(GetAllCategories request, CancellationToken cancellationToken)
    {
        return Task.FromResult(CategoryStubData.Data);
    }
}

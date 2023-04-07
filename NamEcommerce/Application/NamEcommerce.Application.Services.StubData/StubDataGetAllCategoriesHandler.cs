using MediatR;
using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;

namespace NamEcommerce.Application.Services.StubData;

public sealed class StubDataGetAllCategoriesHandler : IRequestHandler<GetAllCategories, IEnumerable<CategoryDto>>
{
    public Task<IEnumerable<CategoryDto>> Handle(GetAllCategories request, CancellationToken cancellationToken)
    {
        return Task.FromResult(CategoryStubData.Data);
    }
}

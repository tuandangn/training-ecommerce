using MediatR;
using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;

namespace NamEcommerce.Application.Services.StubData;

public sealed class StubGetCategoriesByIdsHandler : IRequestHandler<GetCategoriesByIds, IEnumerable<CategoryDto>>
{
    public Task<IEnumerable<CategoryDto>> Handle(GetCategoriesByIds request, CancellationToken cancellationToken)
    {
        return Task.FromResult<IEnumerable<CategoryDto>>(CategoryStubData.Data.Where(c => request.Ids.Contains(c.Id)));
    }
}

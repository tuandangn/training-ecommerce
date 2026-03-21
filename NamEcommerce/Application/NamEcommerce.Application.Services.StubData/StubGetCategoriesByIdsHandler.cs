using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Queries.Catalog;

namespace NamEcommerce.Application.Services.StubData;

public sealed class StubGetCategoriesByIdsHandler : IRequestHandler<GetCategoriesByIds, IEnumerable<CategoryAppDto>>
{
    public Task<IEnumerable<CategoryAppDto>> Handle(GetCategoriesByIds request, CancellationToken cancellationToken)
    {
        return Task.FromResult<IEnumerable<CategoryAppDto>>(CategoryStubData.Data.Where(c => request.Ids.Contains(c.Id)));
    }
}

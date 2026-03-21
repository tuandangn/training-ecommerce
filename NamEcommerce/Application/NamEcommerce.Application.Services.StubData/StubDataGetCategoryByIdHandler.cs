using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Queries.Catalog;

namespace NamEcommerce.Application.Services.StubData;

public sealed class StubDataGetCategoryByIdHandler : IRequestHandler<GetCategoryById, CategoryAppDto?>
{
    public Task<CategoryAppDto?> Handle(GetCategoryById request, CancellationToken cancellationToken)
    {
        return Task.FromResult(CategoryStubData.Data.FirstOrDefault(c => c.Id == request.Id));
    }
}

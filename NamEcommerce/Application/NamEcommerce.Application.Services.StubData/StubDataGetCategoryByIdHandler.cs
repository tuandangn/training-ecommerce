using MediatR;
using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;

namespace NamEcommerce.Application.Services.StubData;

public sealed class StubDataGetCategoryByIdHandler : IRequestHandler<GetCategoryById, CategoryDto?>
{
    public Task<CategoryDto?> Handle(GetCategoryById request, CancellationToken cancellationToken)
    {
        return Task.FromResult(CategoryStubData.Data.FirstOrDefault(c => c.Id == request.Id));
    }
}

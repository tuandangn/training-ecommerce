using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Queries.Catalog;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Queries.Handlers.Catalog;

public sealed class GetCategoryByIdHandler : IRequestHandler<GetCategoryById, CategoryAppDto?>
{
    private readonly IEntityDataReader<Category> _categoryDataReader;

    public GetCategoryByIdHandler(IEntityDataReader<Category> categoryDataReader)
    {
        _categoryDataReader = categoryDataReader;
    }

    public async Task<CategoryAppDto?> Handle(GetCategoryById request, CancellationToken cancellationToken)
    {
        var category = await _categoryDataReader.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
            return null;
        return category.ToDto();
    }
}

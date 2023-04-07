using MediatR;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Application.Shared.Queries.Models.Catalog;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Queries.Handlers.Catalog;

public sealed class GetCategoryByIdHandler : IRequestHandler<GetCategoryById, CategoryDto?>
{
    private readonly IEntityDataReader<Category> _categoryDataReader;

    public GetCategoryByIdHandler(IEntityDataReader<Category> categoryDataReader)
    {
        _categoryDataReader = categoryDataReader;
    }

    public async Task<CategoryDto?> Handle(GetCategoryById request, CancellationToken cancellationToken)
    {
        var category = await _categoryDataReader.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
            return null;
        return category.ToDto();
    }
}

using NamEcommerce.Application.Shared.Dtos.Catalog;

namespace NamEcommerce.Application.Services.Extensions;

public static class CategoryDtoExtensions
{
    public static CategoryDto ToDto(this Domain.Shared.Dtos.Catalog.CategoryDto category)
        => new(category.Id, category.Name)
        {
            DisplayOrder = category.DisplayOrder,
            ParentId = category.ParentId
        };
}

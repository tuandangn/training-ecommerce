using NamEcommerce.Application.Contracts.Dtos.Catalog;

namespace NamEcommerce.Application.Services.Extensions;

public static class CategoryDtoExtensions
{
    public static CategoryAppDto ToDto(this Domain.Shared.Dtos.Catalog.CategoryDto category)
        => new(category.Id, category.Name)
        {
            DisplayOrder = category.DisplayOrder,
            ParentId = category.ParentId
        };
}

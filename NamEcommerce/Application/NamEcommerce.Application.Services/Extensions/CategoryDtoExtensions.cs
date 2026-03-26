using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Application.Services.Extensions;

public static class CategoryDtoExtensions
{
    public static CategoryAppDto ToDto(this CategoryDto category)
        => new(category.Id)
        {
            Name = category.Name,
            DisplayOrder = category.DisplayOrder,
            ParentId = category.ParentId
        };
}

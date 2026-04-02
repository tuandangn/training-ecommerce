using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Application.Services.Extensions;

public static class CategoryExtensions
{
    public static CategoryAppDto ToDto(this Category category)
        => new(category.Id)
        {
            Name = category.Name,
            DisplayOrder = category.DisplayOrder,
            ParentId = category.ParentId
        };

    public static CategoryAppDto ToDto(this CategoryDto category)
        => new(category.Id)
        {
            Name = category.Name,
            DisplayOrder = category.DisplayOrder,
            ParentId = category.ParentId
        };
}

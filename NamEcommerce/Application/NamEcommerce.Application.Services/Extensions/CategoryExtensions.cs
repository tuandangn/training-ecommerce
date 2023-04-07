using NamEcommerce.Application.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Entities.Catalog;

namespace NamEcommerce.Application.Services.Extensions;

public static class CategoryExtensions
{
    public static CategoryDto ToDto(this Category category)
        => new(category.Id, category.Name)
        {
            DisplayOrder = category.DisplayOrder,
            ParentId = category.ParentId
        };
}

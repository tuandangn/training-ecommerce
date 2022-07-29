using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Domain.Services.Extensions;

public static class CategoryExtensions
{
    public static CategoryDto ToDto(this Category category)
        => new(category.Id, category.Name)
        {
            DisplayOrder = category.DisplayOrder,
            ParentId = category.ParentId,
            OnParentDisplayOrder = category.OnParentDisplayOrder
        };
}

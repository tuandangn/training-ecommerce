using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Domain.Services.Extensions;

public static class ProductExtensions
{
    public static ProductDto ToDto(this Product product)
        => new ProductDto(product.Id)
        {
            Name = product.Name,
            ShortDesc = product.ShortDesc,
            Categories = product.ProductCategories.Select(pc => new ProductCategoryDto(pc.CategoryId, pc.DisplayOrder)),
            Pictures = product.ProductPictures.OrderBy(pp => pp.DisplayOrder).Select(pp => pp.PictureId)
        };
}

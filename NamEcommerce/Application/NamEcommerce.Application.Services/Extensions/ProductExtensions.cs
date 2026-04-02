using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Application.Services.Extensions;

public static class ProductExtensions
{
    public static ProductAppDto ToDto(this Product product)
        => new ProductAppDto(product.Id)
        {
            Name = product.Name,
            ShortDesc = product.ShortDesc,
            UnitMeasurementId = product.UnitMeasurementId,
            Categories = product.ProductCategories.Select(pc => new ProductCategoryAppDto(pc.CategoryId, pc.DisplayOrder)),
            Pictures = product.ProductPictures.OrderBy(pp => pp.DisplayOrder).Select(pp => pp.PictureId),
            TrackInventory = product.TrackInventory
        };

    public static ProductAppDto ToDto(this ProductDto dto)
        => new ProductAppDto(dto.Id)
        {
            Name = dto.Name,
            ShortDesc = dto.ShortDesc,
            UnitMeasurementId = dto.UnitMeasurementId,
            Categories = dto.Categories.Select(pc => new ProductCategoryAppDto(pc.CategoryId, pc.DisplayOrder)),
            Pictures = dto.Pictures,
            TrackInventory = dto.TrackInventory
        };
}

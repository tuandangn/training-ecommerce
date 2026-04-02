namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class ProductRepository
{
    public static Mock<IRepository<Product>> CreateProductWillReturns(Product @return)
    {
        return Repository.Create<Product>().WhenCall(r =>
            r.InsertAsync(It.Is<Product>(entity =>
                entity.Name == @return.Name && entity.ShortDesc == @return.ShortDesc
                && entity.UnitMeasurementId == @return.UnitMeasurementId && entity.TrackInventory == @return.TrackInventory
                && entity.ProductCategories.All(pc => @return.ProductCategories.Any(i => i.CategoryId == pc.CategoryId && i.DisplayOrder == pc.DisplayOrder))
                && entity.ProductCategories.Count() == @return.ProductCategories.Count()
                && entity.ProductPictures.All(pp => @return.ProductPictures.Any(i => i.PictureId == pp.PictureId))
                && entity.ProductPictures.Count() == @return.ProductPictures.Count()
            ))
        , @return);
    }
    public static Mock<IRepository<Product>> UpdateProductWillReturns(Product @return)
    {
        return Repository.Create<Product>().WhenCall(r =>
            r.UpdateAsync(It.Is<Product>(entity =>
                entity.Name == @return.Name && entity.ShortDesc == @return.ShortDesc
                && entity.UnitMeasurementId == @return.UnitMeasurementId && entity.TrackInventory == @return.TrackInventory
                && entity.ProductCategories.All(pc => @return.ProductCategories.Any(i => i.CategoryId == pc.CategoryId && i.DisplayOrder == pc.DisplayOrder))
                && entity.ProductCategories.Count() == @return.ProductCategories.Count()
                && entity.ProductPictures.All(pp => @return.ProductPictures.Any(i => i.PictureId == pp.PictureId))
                && entity.ProductPictures.Count() == @return.ProductPictures.Count()
            ))
        , @return);
    }

    public static Mock<IRepository<Product>> NotFound(Guid id)
        => Repository.Create<Product>().WhenCall(r => r.GetByIdAsync(id, default), (Product?)null);
    public static Mock<IRepository<Product>> NotFound(this Mock<IRepository<Product>> mock, Guid id)
        => mock.WhenCall(r => r.GetByIdAsync(id, default), (Product?)null);

    public static Mock<IRepository<Product>> ProductById(Guid id, Product @return)
        => Repository.Create<Product>().WhenCall(r => r.GetByIdAsync(id, default), @return);
    public static Mock<IRepository<Product>> ProductById(this Mock<IRepository<Product>> mock, Guid id, Product @return)
        => mock.WhenCall(r => r.GetByIdAsync(id, default), @return);

    public static Mock<IRepository<Product>> CanDeleteProduct(Product product)
        => Repository.Create<Product>().CanCall(r => r.DeleteAsync(product));
}

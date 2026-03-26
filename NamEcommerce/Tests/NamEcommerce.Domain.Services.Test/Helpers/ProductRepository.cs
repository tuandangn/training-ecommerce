namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class ProductRepository
{
    public static Mock<IRepository<Product>> CreateProductWillReturns(Product @return)
        //*TODO* check inserting data
        => Repository.Create<Product>().WhenCall(r => 
            r.InsertAsync(It.Is<Product>(entity => 
                entity.Name == @return.Name && entity.ShortDesc == @return.ShortDesc))
        , @return);

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

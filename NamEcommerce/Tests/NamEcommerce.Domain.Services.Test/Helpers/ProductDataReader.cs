using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class ProductDataReader
{
    public static Mock<IEntityDataReader<Product>> Empty()
        => EntityDataReader.Create<Product>().WithData(Array.Empty<Product>());

    public static Mock<IEntityDataReader<Product>> WithData(params Product[] products)
        => EntityDataReader.Create<Product>().WithData(products);
    public static Mock<IEntityDataReader<Product>> HasOne(Product product)
        => EntityDataReader.Create<Product>().WithData(product);

    public static Mock<IEntityDataReader<Product>> NotFound(Guid id)
        => EntityDataReader.Create<Product>().WhenCall(reader => reader.GetByIdAsync(id), (Product?)null);

    public static Mock<IEntityDataReader<Product>> ProductById(Guid id, Product product)
        => EntityDataReader.Create<Product>().WhenCall(reader => reader.GetByIdAsync(id), product);
    public static Mock<IEntityDataReader<Product>> ProductById(this Mock<IEntityDataReader<Product>> mock, Guid id, Product product)
        => mock.WhenCall(reader => reader.GetByIdAsync(id), product);
}

using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Data.Contracts;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class ProductPriceHistoryRepository
{
    public static Mock<IRepository<ProductPriceHistory>> Create()
        => new Mock<IRepository<ProductPriceHistory>>();
}

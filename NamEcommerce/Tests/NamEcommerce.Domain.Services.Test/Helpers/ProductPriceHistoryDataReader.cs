using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class ProductPriceHistoryDataReader
{
    public static Mock<IEntityDataReader<ProductPriceHistory>> Empty()
        => EntityDataReader.Create<ProductPriceHistory>().WithData(Array.Empty<ProductPriceHistory>());

    public static Mock<IEntityDataReader<ProductPriceHistory>> WithData(params ProductPriceHistory[] history)
        => EntityDataReader.Create<ProductPriceHistory>().WithData(history);
}

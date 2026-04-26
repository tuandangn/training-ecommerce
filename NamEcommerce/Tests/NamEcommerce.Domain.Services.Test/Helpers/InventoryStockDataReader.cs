using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class InventoryStockDataReader
{
    public static Mock<IEntityDataReader<InventoryStock>> Empty()
        => EntityDataReader.Create<InventoryStock>().WithData(Array.Empty<InventoryStock>());

    public static Mock<IEntityDataReader<InventoryStock>> WithData(params InventoryStock[] stocks)
        => EntityDataReader.Create<InventoryStock>().WithData(stocks);

    public static Mock<IEntityDataReader<InventoryStock>> HasOne(InventoryStock stock)
        => EntityDataReader.Create<InventoryStock>().WithData(stock);
}

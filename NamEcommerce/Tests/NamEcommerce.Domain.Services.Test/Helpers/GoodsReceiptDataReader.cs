using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class GoodsReceiptDataReader
{
    public static Mock<IEntityDataReader<GoodsReceipt>> Empty()
        => EntityDataReader.Create<GoodsReceipt>().WithData(Array.Empty<GoodsReceipt>());

    public static Mock<IEntityDataReader<GoodsReceipt>> WithData(params GoodsReceipt[] goodsReceipts)
        => EntityDataReader.Create<GoodsReceipt>().WithData(goodsReceipts);

    public static Mock<IEntityDataReader<GoodsReceipt>> HasOne(GoodsReceipt goodsReceipt)
        => EntityDataReader.Create<GoodsReceipt>().WithData(goodsReceipt);

    public static Mock<IEntityDataReader<GoodsReceipt>> NotFound(Guid id)
        => EntityDataReader.Create<GoodsReceipt>().WhenCall(reader => reader.GetByIdAsync(id), (GoodsReceipt?)null);

    public static Mock<IEntityDataReader<GoodsReceipt>> GoodsReceiptById(Guid id, GoodsReceipt goodsReceipt)
        => EntityDataReader.Create<GoodsReceipt>().WhenCall(reader => reader.GetByIdAsync(id), goodsReceipt);

    public static Mock<IEntityDataReader<GoodsReceipt>> GoodsReceiptById(this Mock<IEntityDataReader<GoodsReceipt>> mock, Guid id, GoodsReceipt goodsReceipt)
        => mock.WhenCall(reader => reader.GetByIdAsync(id), goodsReceipt);
}

using NamEcommerce.Domain.Entities.GoodsReceipts;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class GoodsReceiptRepository
{
    public static Mock<IRepository<GoodsReceipt>> InsertWillReturn(GoodsReceipt @return)
        => Repository.Create<GoodsReceipt>().WhenCall(r =>
            r.InsertAsync(It.Is<GoodsReceipt>(entity =>
                entity.TruckDriverName == @return.TruckDriverName
                && entity.TruckNumberSerial == @return.TruckNumberSerial
                && entity.ReceivedOnUtc == @return.ReceivedOnUtc
                && entity.PictureIds.Count == @return.PictureIds.Count
                && entity.Items.Count == @return.Items.Count))
        , @return);

    public static Mock<IRepository<GoodsReceipt>> UpdateWillReturn(GoodsReceipt @return)
        => Repository.Create<GoodsReceipt>().WhenCall(r =>
            r.UpdateAsync(It.Is<GoodsReceipt>(entity =>
                entity.Id == @return.Id
                && entity.TruckDriverName == @return.TruckDriverName
                && entity.TruckNumberSerial == @return.TruckNumberSerial))
        , @return);

    public static Mock<IRepository<GoodsReceipt>> CanDelete(GoodsReceipt goodsReceipt)
        => Repository.Create<GoodsReceipt>().CanCall(r => r.DeleteAsync(goodsReceipt));
}

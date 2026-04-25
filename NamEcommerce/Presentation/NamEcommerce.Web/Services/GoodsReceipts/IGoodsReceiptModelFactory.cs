using NamEcommerce.Web.Contracts.Models.GoodsReceipts;
using NamEcommerce.Web.Models.GoodsReceipts;

namespace NamEcommerce.Web.Services.GoodsReceipts;

public interface IGoodsReceiptModelFactory
{
    Task<GoodsReceiptListModel> PrepareGoodsReceiptListModel(GoodsReceiptListSearchModel searchModel);
    Task<CreateGoodsReceiptModel> PrepareCreateGoodsReceiptModel(CreateGoodsReceiptModel? model = null);
    Task<GoodsReceiptDetailsModel?> PrepareGoodsReceiptDetailsModel(Guid id);
}

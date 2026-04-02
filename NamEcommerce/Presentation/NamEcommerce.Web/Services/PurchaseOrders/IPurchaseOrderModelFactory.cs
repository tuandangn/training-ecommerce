using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Models.PurchaseOrders;

namespace NamEcommerce.Web.Services.PurchaseOrders;

public interface IPurchaseOrderModelFactory
{
    Task<PurchaseOrderListModel> PreparePurchaseOrderListModel(PurchaseOrderListSearchModel searchModel);

    Task<PurchaseOrderDetailsModel?> PreparePurchaseOrderDetailsModel(Guid id);

    Task<CreatePurchaseOrderModel> PrepareCreatePurchaseOrderModel(CreatePurchaseOrderModel? model = null);
}

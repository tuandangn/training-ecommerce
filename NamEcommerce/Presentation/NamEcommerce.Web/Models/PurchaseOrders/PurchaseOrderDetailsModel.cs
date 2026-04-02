using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderDetailsModel
{
    [ValidateNever]
    public required PurchaseOrderModel Info { get; init; }

    public bool CanModifyInfo { get; set; }
    public EditPurchaseOrderModel ModifyInfo { get; set; }

    [ValidateNever]
    public AddPurchaseOrderItemModel? AddItemModel { get; set; }

    [ValidateNever]
    public IList<ReceivePurchaseOrderItemModel> ReceiveItemModels { get; set; } = [];

    [ValidateNever]
    public required EntityOptionListModel AvailableWarehouses { get; set; }
}

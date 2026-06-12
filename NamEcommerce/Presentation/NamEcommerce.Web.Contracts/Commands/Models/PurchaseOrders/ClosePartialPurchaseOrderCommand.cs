using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class ClosePartialPurchaseOrderCommand : ICommand<CommonActionResultModel>
{
    public Guid PurchaseOrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.PurchaseOrders;

[Serializable]
public sealed class DeletePurchaseOrderItemModel
{
    [Required]
    public Guid PurchaseOrderId { get; set; }

    [Required]
    public Guid PurchaseOrderItemId { get; set; }
}

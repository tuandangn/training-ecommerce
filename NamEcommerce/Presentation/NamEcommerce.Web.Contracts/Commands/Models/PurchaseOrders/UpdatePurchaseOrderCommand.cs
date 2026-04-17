using MediatR;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class UpdatePurchaseOrderCommand : IRequest<UpdatePurchaseOrderResultModel>
{
    public required Guid Id { get; set; }
    public required Guid? VendorId { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? Note { get; set; }
    public required decimal TaxAmount { get; set; }
    public required decimal ShippingAmount { get; set; }
}

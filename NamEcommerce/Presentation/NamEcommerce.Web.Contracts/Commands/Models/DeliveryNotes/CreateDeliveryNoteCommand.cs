using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

[Serializable]
public sealed class CreateDeliveryNoteCommand : IRequest<CommonActionResultModel>
{
    public Guid OrderId { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public bool ShowPrice { get; set; }
    public string? Note { get; set; }
    public decimal Surcharge { get; set; }
    public string? SurchargeReason { get; set; }
    public decimal AmountToCollect { get; set; }

    public IList<CreateDeliveryNoteItemModel> Items { get; set; } = [];

    [Serializable]
    public sealed class CreateDeliveryNoteItemModel
    {
        public Guid OrderItemId { get; set; }
        public decimal Quantity { get; set; }
    }
}

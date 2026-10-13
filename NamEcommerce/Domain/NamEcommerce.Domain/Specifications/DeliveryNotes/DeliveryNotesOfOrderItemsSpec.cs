using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.DeliveryNotes;

[Serializable]
public sealed class DeliveryNotesOfOrderItemsSpec : BaseSpecification<DeliveryNote>
{
    public DeliveryNotesOfOrderItemsSpec(Guid orderId, IList<Guid> orderItemIds) 
        : base(deliveryNote => deliveryNote.OrderId == orderId && deliveryNote.Items.Any(item => orderItemIds.Contains(item.OrderItemId)))
    {
    }

    internal DeliveryNotesOfOrderItemsSpec(IList<Guid> orderItemIds) 
        : base(deliveryNote => deliveryNote.Items.Any(item => orderItemIds.Contains(item.OrderItemId)))
    {
    }
}
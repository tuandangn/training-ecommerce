using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.DeliveryNotes;

[Serializable]
public sealed class ActiveDeliveryNotesOfOrderItemsSpec : BaseSpecification<DeliveryNote>
{
    public ActiveDeliveryNotesOfOrderItemsSpec(Guid orderId, IList<Guid> orderItemIds) 
        : base(new DeliveryNotesOfOrderItemsSpec(orderId, orderItemIds).Criteria.AndNot(new HaveStatusDeliveryNoteSpec([DeliveryNoteStatus.Cancelled]).Criteria))
    {
    }
    internal ActiveDeliveryNotesOfOrderItemsSpec(IList<Guid> orderItemIds) 
        : base(new DeliveryNotesOfOrderItemsSpec(orderItemIds).Criteria.AndNot(new HaveStatusDeliveryNoteSpec([DeliveryNoteStatus.Cancelled]).Criteria))
    {
    }
}
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.DeliveryNotes;

[Serializable]
public sealed class ActiveDeliveryNotesOfOrderSpec : BaseSpecification<DeliveryNote>
{
    public ActiveDeliveryNotesOfOrderSpec(IList<Guid> orderIds) 
        : base(new DeliveryNotesOfOrdersSpec(orderIds).Criteria.AndNot(new HaveStatusDeliveryNoteSpec([DeliveryNoteStatus.Cancelled]).Criteria))
    {
    }
}
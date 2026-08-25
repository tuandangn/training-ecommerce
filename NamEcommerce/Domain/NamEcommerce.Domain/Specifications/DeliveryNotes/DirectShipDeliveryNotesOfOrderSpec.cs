using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.DeliveryNotes;

[Serializable]
public sealed class DirectShipDeliveryNotesOfOrderSpec(Guid orderId, IList<DeliveryNoteStatus>? notHaveStatus = null) 
    : BaseSpecification<DeliveryNote>((notHaveStatus is not null && notHaveStatus.Count > 0 
        ? new DeliveryNotesOfOrdersSpec([orderId]).Criteria.AndNot(new HaveStatusDeliveryNoteSpec(notHaveStatus).Criteria)
        : new DeliveryNotesOfOrdersSpec([orderId]).Criteria.AndNot(new HaveStatusDeliveryNoteSpec([DeliveryNoteStatus.Cancelled]).Criteria))
        .And(new IsDirectShipDeliveryNoteSpec().Criteria)
    );

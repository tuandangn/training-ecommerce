using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.DeliveryNotes;

[Serializable]
public sealed class ActiveDeliveryNotesOfOrderItemsSpec(Guid orderId, IList<Guid> orderItemIds) : BaseSpecification<DeliveryNote>(
    new DeliveryNotesOfOrderItemsSpec(orderId, orderItemIds).Criteria
    .AndNot(new HaveStatusDeliveryNoteSpec([DeliveryNoteStatus.Cancelled]).Criteria));

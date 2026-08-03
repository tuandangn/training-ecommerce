using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.DeliveryNotes;

[Serializable]
public sealed class HaveIdsDeliveryNoteSpec(IList<Guid> deliveryIds) 
    : BaseSpecification<DeliveryNote>(deliveryNote => deliveryIds.Contains(deliveryNote.Id));

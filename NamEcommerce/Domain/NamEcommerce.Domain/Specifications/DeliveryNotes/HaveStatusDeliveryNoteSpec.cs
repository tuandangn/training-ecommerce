using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.DeliveryNotes;

[Serializable]
public sealed class HaveStatusDeliveryNoteSpec(IList<DeliveryNoteStatus> status) 
    : BaseSpecification<DeliveryNote>(deliveryNote => status.Contains(deliveryNote.Status));

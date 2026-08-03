using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.DeliveryNotes;

[Serializable]
public sealed class DeliveryNotesOfOrdersSpec(IList<Guid> orderIds) : BaseSpecification<DeliveryNote>(deliveryNote => orderIds.Contains(deliveryNote.OrderId));

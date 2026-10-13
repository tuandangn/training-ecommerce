using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.DeliveryNotes;

[Serializable]
public sealed class IsDirectShipDeliveryNoteSpec() 
    : BaseSpecification<DeliveryNote>(deliveryNote => deliveryNote.IsDirectShip && deliveryNote.SourceType == DeliveryNoteSourceType.DirectShipToCustomer);

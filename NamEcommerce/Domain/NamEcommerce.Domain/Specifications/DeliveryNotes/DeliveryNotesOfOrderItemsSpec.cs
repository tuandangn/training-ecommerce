using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.DeliveryNotes;

[Serializable]
public sealed class DeliveryNotesOfOrderItemsSpec(Guid orderId, IList<Guid> orderItemIds) : BaseSpecification<DeliveryNote>(
    deliveryNote => deliveryNote.OrderId == orderId && deliveryNote.Items.Any(item => orderItemIds.Contains(item.OrderItemId)));

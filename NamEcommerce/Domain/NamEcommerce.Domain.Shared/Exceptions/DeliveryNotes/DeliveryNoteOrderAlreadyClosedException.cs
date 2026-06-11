using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;

public sealed class DeliveryNoteOrderAlreadyClosedException : NamEcommerceDomainException
{
    public DeliveryNoteOrderAlreadyClosedException(Guid orderId, OrderStatus orderStatus)
        : base("Error.DeliveryNoteOrderAlreadyClosed", orderId, orderStatus)
    {
    }
}

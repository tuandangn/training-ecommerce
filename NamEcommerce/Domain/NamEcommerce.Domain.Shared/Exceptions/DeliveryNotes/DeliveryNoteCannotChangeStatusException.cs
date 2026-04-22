using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

namespace NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;

public sealed class DeliveryNoteCannotChangeStatusException : NamEcommerceDomainException
{
    public DeliveryNoteCannotChangeStatusException(DeliveryNoteStatus currentStatus, DeliveryNoteStatus newStatus)
        : base("Error.DeliveryNoteCannotChangeStatus", currentStatus, newStatus)
    {
    }
}


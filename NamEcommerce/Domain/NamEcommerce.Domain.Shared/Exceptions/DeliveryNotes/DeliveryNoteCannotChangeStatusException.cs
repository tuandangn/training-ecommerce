using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

namespace NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;

public sealed class DeliveryNoteCannotChangeStatusException : Exception
{
    public DeliveryNoteCannotChangeStatusException(DeliveryNoteStatus currentStatus, DeliveryNoteStatus newStatus)
        : base($"Cannot change delivery note status from {currentStatus} to {newStatus}.")
    {
    }
}

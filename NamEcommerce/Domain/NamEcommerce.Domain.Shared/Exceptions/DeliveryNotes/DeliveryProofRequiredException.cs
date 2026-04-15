namespace NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;

public sealed class DeliveryProofRequiredException : Exception
{
    public DeliveryProofRequiredException()
        : base("Delivery proof picture is required when marking a delivery note as delivered.")
    {
    }
}

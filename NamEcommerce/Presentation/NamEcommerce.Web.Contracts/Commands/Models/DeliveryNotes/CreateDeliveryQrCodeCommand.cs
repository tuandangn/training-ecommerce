using NamEcommerce.Web.Contracts.Models.DeliveryNotes;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

[Serializable]
public sealed record CreateDeliveryQrCodeCommand(Guid DeliveryNoteId) : IRequest<CustomerPortalDeliveryQrCodeModel?>
{
    public required string CustomerPortalUrl { get; set; }
}

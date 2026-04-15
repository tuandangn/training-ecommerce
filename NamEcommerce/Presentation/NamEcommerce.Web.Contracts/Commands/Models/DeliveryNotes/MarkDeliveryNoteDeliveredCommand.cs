using MediatR;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

public sealed record MarkDeliveryNoteDeliveredResult(bool Success, string? ErrorMessage = null);

public sealed class MarkDeliveryNoteDeliveredCommand : IRequest<MarkDeliveryNoteDeliveredResult>
{
    public Guid DeliveryNoteId { get; init; }
    public string? ReceiverName { get; init; }
    
    // We pass raw bytes or use similar approach to MarkOrderItemDeliveredCommand
    public byte[]? PictureData { get; init; }
    public string? PictureContentType { get; init; }
    public string? PictureFileName { get; init; }
}

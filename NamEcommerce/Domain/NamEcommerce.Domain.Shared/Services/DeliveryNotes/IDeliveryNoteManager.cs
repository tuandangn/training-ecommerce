using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

namespace NamEcommerce.Domain.Shared.Services.DeliveryNotes;

public interface IDeliveryNoteManager
{
    Task<DeliveryNoteDto?> GetByIdAsync(Guid id);
    Task<IPagedDataDto<DeliveryNoteDto>> GetDeliveryNotesAsync(int pageIndex, int pageSize, 
        string? keywords = null, Guid? orderId = null, IEnumerable<DeliveryNoteStatus>? status = null);
    Task<IDictionary<Guid, decimal>> GetDeliveredQuantitiesAsync(IEnumerable<Guid> orderItemIds);
    Task<Guid?> GetWaitingPaymentDeliveryNoteIdAsync(Guid orderId);

    Task<IDictionary<Guid, List<DeliveryNoteLinkDto>>> GetDeliveryNoteLinksAsync(IEnumerable<Guid> orderItemIds);
    Task<DeliveryNoteDto> CreateFromOrderAsync(CreateDeliveryNoteDto dto);
    Task<Guid> CreateAsDeliveredAsync(CreateDeliveryNoteFromVendorReturnDto dto);
    Task<Guid> CreateForDirectShipAsync(CreateDeliveryNoteForDirectShipDto dto, CancellationToken ct = default);
    
    Task UpdateShippingAsync(UpdateDeliveryNoteShippingDto dto);
    Task AdminUpdateAmountToCollectAsync(Guid deliveryNoteId, decimal newAmount, string? note, Guid? adminUserId);
    Task ConfirmAsync(Guid id);
    Task CancelAsync(Guid id);
    Task ConfirmDirectShipDeliveryAsync(Guid id, DateTime confirmedAtUtc, string? note);
    Task RejectDirectShipDeliveryAsync(Guid id, string reason);
    
    Task MarkDeliveringAsync(Guid id);
    Task MarkDeliveredAsync(MarkDeliveryNoteDeliveredDto dto);
    Task MarkPendingConfirmationAsync(MarkDeliveryNoteDeliveredDto dto);
    Task AssignDeliveryUserAsync(AssignDeliveryUserDto dto);
    Task MarkReceivedByCustomerAsync(Guid id, DateTime receivedAtUtc, string? receiverName,
        string? note, DeliveryAcceptanceDto? acceptance = null);
    Task MarkAsOrderIsPaid(Guid deliveryNoteId);
    
    Task RequestSettlementApprovalAsync(RequestDeliverySettlementDto dto);
    Task ApproveSettlementAsync(ApproveDeliverySettlementDto dto);
    Task RejectSettlementAsync(Guid id, string reason, Guid? approvedByUserId);
    Task CompleteApprovedSettlementAsync(Guid id,
        IReadOnlyList<Guid> pictureIds, DeliveryCompletionMetadataDto? completionMetadata);
}

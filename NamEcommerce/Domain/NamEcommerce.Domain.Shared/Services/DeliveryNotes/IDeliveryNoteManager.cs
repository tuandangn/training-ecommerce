using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

namespace NamEcommerce.Domain.Shared.Services.DeliveryNotes;

public interface IDeliveryNoteManager
{
    Task<DeliveryNoteDto> CreateFromOrderAsync(CreateDeliveryNoteDto dto);
    
    Task ConfirmAsync(Guid id);
    
    Task MarkDeliveringAsync(Guid id);
    
    Task MarkDeliveredAsync(MarkDeliveryNoteDeliveredDto dto);

    Task MarkPendingConfirmationAsync(MarkDeliveryNoteDeliveredDto dto);

    Task AssignDeliveryUserAsync(AssignDeliveryUserDto dto);

    Task UpdateShippingAsync(UpdateDeliveryNoteShippingDto dto);

    Task MarkReceivedByCustomerAsync(
        Guid id,
        DateTime receivedAtUtc,
        string? receiverName,
        string? note,
        DeliveryAcceptanceDto? acceptance = null);
    
    Task CancelAsync(Guid id);

    Task RequestSettlementApprovalAsync(RequestDeliverySettlementDto dto);

    Task ApproveSettlementAsync(ApproveDeliverySettlementDto dto);

    Task RejectSettlementAsync(Guid id, string reason, Guid? approvedByUserId);

    Task CompleteApprovedSettlementAsync(
        Guid id,
        IReadOnlyList<Guid> pictureIds,
        DeliveryCompletionMetadataDto? completionMetadata);
    
    /// <summary>
    /// Tự động tạo DeliveryNote ở trạng thái Delivered ngay khi VendorReturn được Confirm.
    /// SourceType=ToVendorReturn — handler downstream trừ tồn kho nhưng KHÔNG sinh CustomerDebt.
    /// </summary>
    Task<Guid> CreateAsDeliveredAsync(CreateDeliveryNoteFromVendorReturnDto dto);

    /// <summary>
    /// Tự động tạo DeliveryNote direct-ship ở trạng thái Confirmed để chờ khách xác nhận.
    /// Manager tự tìm Order/Customer/Product từ <paramref name="dto"/>.OrderItemId.
    /// </summary>
    Task<Guid> CreateForDirectShipAsync(CreateDeliveryNoteForDirectShipDto dto, CancellationToken ct = default);

    Task ConfirmDirectShipDeliveryAsync(Guid id, DateTime confirmedAtUtc, string? note, CancellationToken ct = default);

    Task RejectDirectShipDeliveryAsync(Guid id, string reason, CancellationToken ct = default);

    Task<DeliveryNoteDto?> GetByIdAsync(Guid id);
    
    Task<IPagedDataDto<DeliveryNoteDto>> GetDeliveryNotesAsync(int pageIndex, int pageSize, string? keywords = null, Guid? orderId = null, IEnumerable<DeliveryNoteStatus>? status = null);

    Task<IDictionary<Guid, decimal>> GetDeliveredQuantitiesAsync(IEnumerable<Guid> orderItemIds);

    Task<IDictionary<Guid, List<DeliveryNoteLinkDto>>> GetDeliveryNoteLinksAsync(IEnumerable<Guid> orderItemIds);

    Task AdminUpdateAmountToCollectAsync(Guid deliveryNoteId, decimal newAmount, string? note, Guid? adminUserId);
}

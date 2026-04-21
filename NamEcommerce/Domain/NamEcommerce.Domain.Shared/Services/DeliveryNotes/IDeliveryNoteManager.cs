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
    
    Task CancelAsync(Guid id);
    
    Task<DeliveryNoteDto?> GetByIdAsync(Guid id);
    
    Task<IPagedDataDto<DeliveryNoteDto>> GetDeliveryNotesAsync(int pageIndex, int pageSize, string? keywords = null, Guid? orderId = null, IEnumerable<DeliveryNoteStatus>? status = null);

    Task<IDictionary<Guid, decimal>> GetDeliveredQuantitiesAsync(IEnumerable<Guid> orderItemIds);

    Task<IDictionary<Guid, List<DeliveryNoteLinkDto>>> GetDeliveryNoteLinksAsync(IEnumerable<Guid> orderItemIds);
}

using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;

namespace NamEcommerce.Domain.Shared.Services.DeliveryNotes;

public interface IDeliveryNoteManager
{
    Task<DeliveryNoteDto> CreateFromOrderAsync(CreateDeliveryNoteDto dto);
    
    Task ConfirmAsync(Guid id);
    
    Task MarkDeliveringAsync(Guid id);
    
    Task MarkDeliveredAsync(MarkDeliveryNoteDeliveredDto dto);
    
    Task CancelAsync(Guid id);
    
    Task<DeliveryNoteDto?> GetByIdAsync(Guid id);
    
    Task<IPagedDataDto<DeliveryNoteDto>> GetListAsync(
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);
}

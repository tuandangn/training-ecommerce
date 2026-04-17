using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.DeliveryNotes;

public interface IDeliveryNoteAppService
{
    Task<DeliveryNoteAppDto> CreateFromOrderAsync(CreateDeliveryNoteAppDto dto);
    
    Task CancelAsync(Guid id);
    
    Task ConfirmAsync(Guid id);
    
    Task MarkDeliveringAsync(Guid id);
    
    Task<MarkDeliveryNoteDeliveredResultAppDto> MarkDeliveredAsync(MarkDeliveryNoteDeliveredAppDto dto);
    
    Task<DeliveryNoteAppDto?> GetByIdAsync(Guid id);
    
    Task<IList<DeliveryNoteAppDto>> GetByOrderIdAsync(Guid orderId);
    
    Task<PagedDataAppDto<DeliveryNoteAppDto>> GetListAsync(string? keywords = null, int pageIndex = 0, int pageSize = 15);
}

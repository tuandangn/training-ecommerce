using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.DeliveryNotes;

public interface IDeliveryNoteAppService
{
    Task<CreateDeliveryNoteResultAppDto> CreateFromOrderAsync(CreateDeliveryNoteAppDto dto);

    Task<CreateDeliveryNoteResultAppDto> CreateAsDeliveredFromVendorReturnAsync(CreateDeliveryNoteFromVendorReturnAppDto dto);
    
    Task CancelAsync(Guid id);
    
    Task ConfirmAsync(Guid id);
    
    Task<CommonActionResultDto> MarkDeliveringAsync(Guid id);
    
    Task<MarkDeliveryNoteDeliveredResultAppDto> MarkDeliveredAsync(MarkDeliveryNoteDeliveredAppDto dto);

    Task<AssignDeliveryUserResultAppDto> AssignDeliveryUserAsync(AssignDeliveryUserAppDto dto);
    
    Task<DeliveryNoteAppDto?> GetByIdAsync(Guid id);
    
    Task<IList<DeliveryNoteAppDto>> GetByOrderIdAsync(Guid orderId);
    
    Task<PagedDataAppDto<DeliveryNoteAppDto>> GetListAsync(string? keywords = null, int pageIndex = 0, int pageSize = 15);
}

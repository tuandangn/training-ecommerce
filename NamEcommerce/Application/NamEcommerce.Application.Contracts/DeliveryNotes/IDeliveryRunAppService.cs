using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;

namespace NamEcommerce.Application.Contracts.DeliveryNotes;

public interface IDeliveryRunAppService
{
    Task<CreateDeliveryRunResultAppDto> CreateAsync(CreateDeliveryRunAppDto dto);
    Task<CommonActionResultDto> AcknowledgeDriverCacheAsync(Guid id, string? deviceId);
    Task<CommonActionResultDto> IssuePaperManifestAsync(Guid id);
    Task<CommonActionResultDto> HandOverAsync(Guid id);
    Task<CommonActionResultDto> CloseAsync(Guid id);
    Task<CommonActionResultDto> ConfirmCashHandoverAsync(ConfirmDeliveryRunCashHandoverAppDto dto);
    Task<CommonActionResultDto> CancelAsync(Guid id);
    Task<DeliveryRunAppDto?> GetByIdAsync(Guid id);
    Task<DeliveryRunAppDto?> GetByDeliveryNoteIdAsync(Guid deliveryNoteId);
    Task<IPagedDataAppDto<DeliveryRunListAppDto>> GetListAsync(int pageIndex, int pageSize,
        string? keywords, Guid? assignedDeliveryUserId, int? status);
}

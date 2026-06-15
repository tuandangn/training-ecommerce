using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

namespace NamEcommerce.Domain.Shared.Services.DeliveryNotes;

public interface IDeliveryRunManager
{
    Task<DeliveryRunDto> CreateAsync(CreateDeliveryRunDto dto);
    Task AcknowledgeDriverCacheAsync(Guid id, string? deviceId);
    Task IssuePaperManifestAsync(Guid id);
    Task HandOverAsync(Guid id);
    Task CloseAsync(Guid id);
    Task ConfirmCashHandoverAsync(ConfirmDeliveryRunCashHandoverDto dto);
    Task UpdateDeliveredNoteCashCollectedAsync(UpdateDeliveryRunDeliveredNoteCashCollectedDto dto);
    Task CancelAsync(Guid id);
    Task<DeliveryRunDto?> GetByIdAsync(Guid id);
    Task<DeliveryRunDto?> GetByDeliveryNoteIdAsync(Guid deliveryNoteId);
    Task<IPagedDataDto<DeliveryRunDto>> GetListAsync(int pageIndex, int pageSize, string? keywords,
        Guid? assignedDeliveryUserId, DeliveryRunStatus? status);
}

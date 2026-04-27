using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

namespace NamEcommerce.Application.Contracts.PurchaseOrders;

public interface IPurchaseOrderAppService
{
    Task<IPagedDataAppDto<PurchaseOrderAppDto>> GetPurchaseOrdersAsync(string? keywords, int pageIndex, int pageSize);

    Task<string> NextPurchaseOrderCodeAsync();

    Task<PurchaseOrderAppDto?> GetPurchaseOrderByIdAsync(Guid id);

    Task<CreatePurchaseOrderResultAppDto> CreatePurchaseOrderAsync(CreatePurchaseOrderAppDto dto);
    Task<UpdatePurchaseOrderResultAppDto> UpdatePurchaseOrderAsync(UpdatePurchaseOrderAppDto dto);

    Task<CommonActionResultDto> SubmitsPurchaseOrderAsync(Guid id);
    Task<CommonActionResultDto> CancelPurchaseOrderAsync(Guid id);
    Task<CommonActionResultDto> ChangeStatusAsync(Guid purchaseOrderId, int newStatus);

    Task<CommonActionResultDto> AddPurchaseOrderItemAsync(AddPurchaseOrderItemAppDto dto);

    Task<CommonActionResultDto> DeletePurchaseOrderItemAsync(DeletePurchaseOrderItemAppDto dto);

    Task<CommonActionResultDto> ReceiveItemAsync(ReceivedGoodsForItemAppDto dto);

    Task<IList<RecentPurchasePriceAppDto>> GetRecentPurchasePricesAsync(Guid productId);
}

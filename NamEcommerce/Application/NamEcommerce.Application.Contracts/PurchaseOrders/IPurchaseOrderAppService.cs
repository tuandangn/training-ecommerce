using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

namespace NamEcommerce.Application.Contracts.PurchaseOrders;

public interface IPurchaseOrderAppService
{
    Task<IPagedDataAppDto<PurchaseOrderAppDto>> GetPurchaseOrdersAsync(string? keywords, int pageIndex, int pageSize);

    Task<string> NextPurchaseOrderCode();

    Task<PurchaseOrderAppDto?> GetPurchaseOrderByIdAsync(Guid id);

    Task<CreatePurchaseOrderResultAppDto> CreatePurchaseOrderAsync(CreatePurchaseOrderAppDto dto);

    Task<(bool success, string? errorMessage)> SubmitsPurchaseOrderAsync(Guid id);
    Task<(bool success, string? errorMessage)> ChangeStatusAsync(Guid purchaseOrderId, int newStatus);

    Task<AddPurchaseOrderItemResultAppDto> AddPurchaseOrderItemAsync(AddPurchaseOrderItemAppDto dto);

    Task<ReceivedGoodsForItemResultAppDto> ReceiveItemAsync(ReceivedGoodsForItemAppDto dto);
}

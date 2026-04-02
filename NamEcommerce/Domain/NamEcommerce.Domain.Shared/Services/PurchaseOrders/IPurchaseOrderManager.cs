using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Services.PurchaseOrders;

public interface IPurchaseOrderManager : ICodeExistCheckingService
{
    Task<IPagedDataDto<PurchaseOrderDto>> GetPurchaseOrdersAsync(string? keywords, int pageIndex, int pageSize);

    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id);

    Task<CreatePurchaseOrderResultDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto);

    Task ChangeStatusAsync(Guid purchaseOrderId, PurchaseOrderStatus status);

    Task<bool> CanChangeStatusToAsync(Guid purchaseOrderId, PurchaseOrderStatus status);
    Task<bool> CanAddPurchaseOrderItemsAsync(Guid purchaseOrderId);

    Task<bool> CanReceiveGoodsAsync(Guid purchaseOrderId);

    Task VerifyStatusAsync(Guid purchaseOrderId);
    
    Task<AddPurchaseOrderItemResultDto> AddPurchaseOrderItemAsync(AddPurchaseOrderItemDto dto);
    
    Task<ReceivedGoodsForItemResultDto> ReceiveItemsAsync(ReceivedGoodsForItemDto dto);
}

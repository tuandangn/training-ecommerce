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
    Task<UpdatePurchaseOrderResultDto> UpdatePurchaseOrderAsync(UpdatePurchaseOrderDto dto);
    
    Task<AddPurchaseOrderItemResultDto> AddPurchaseOrderItemAsync(AddPurchaseOrderItemDto dto);
    Task DeleteOrderItemAsync(Guid purchaseOrderId, Guid itemId);
    Task<ReceivedGoodsForItemResultDto> ReceiveItemsAsync(ReceivedGoodsForItemDto dto);

    Task ChangeStatusAsync(Guid purchaseOrderId, PurchaseOrderStatus status);
    Task VerifyStatusAsync(Guid purchaseOrderId);

    Task<bool> CanChangeStatusToAsync(Guid purchaseOrderId, PurchaseOrderStatus status);
    Task<bool> CanAddPurchaseOrderItemsAsync(Guid purchaseOrderId);
    Task<bool> CanReceiveGoodsAsync(Guid purchaseOrderId);
}

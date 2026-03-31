using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Services.PurchaseOrders;

public interface IPurchaseOrderManager : ICodeExistCheckingService
{
    Task<CreatePurchaseOrderResultDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto);

    Task ChangeStatusAsync(Guid purchaseOrderId, PurchaseOrderStatus status);

    Task VerifyStatusAsync(Guid purchaseOrderId);
    
    Task<AddPurchaseOrderItemResultDto> AddPurchaseOrderItemAsync(AddPurchaseOrderItemDto dto);
    
    Task<ReceivedGoodsForItemResultDto> ReceiveItemsAsync(ReceivedGoodsForItemDto dto);
}

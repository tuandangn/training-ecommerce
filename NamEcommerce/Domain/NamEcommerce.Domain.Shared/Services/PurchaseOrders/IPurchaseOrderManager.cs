using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Services.PurchaseOrders;

public interface IPurchaseOrderManager : ICodeExistCheckingService
{
    Task<IPagedDataDto<PurchaseOrderDto>> GetPurchaseOrdersAsync(string? keywords, int pageIndex, int pageSize);
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id);

    /// <summary>
    /// Lấy giá nhập gần nhất của một hàng hóa theo từng nhà cung cấp.
    /// Mỗi nhà cung cấp chỉ trả về một bản ghi (lần nhập gần nhất).
    /// Chỉ tính các đơn không bị hủy.
    /// </summary>
    Task<IList<RecentPurchasePriceDto>> GetRecentPurchasePricesAsync(Guid productId);

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

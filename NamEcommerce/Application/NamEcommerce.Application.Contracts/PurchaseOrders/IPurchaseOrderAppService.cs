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

    Task<(bool success, string? errorMessage)> SubmitsPurchaseOrderAsync(Guid id);
    Task<(bool success, string? errorMessage)> CancelPurchaseOrderAsync(Guid id);
    Task<(bool success, string? errorMessage)> ChangeStatusAsync(Guid purchaseOrderId, int newStatus);

    Task<AddPurchaseOrderItemResultAppDto> AddPurchaseOrderItemAsync(AddPurchaseOrderItemAppDto dto);

    Task<DeletePurchaseOrderItemResultAppDto> DeletePurchaseOrderItemAsync(DeletePurchaseOrderItemAppDto dto);

    Task<ReceivedGoodsForItemResultAppDto> ReceiveItemAsync(ReceivedGoodsForItemAppDto dto);

    /// <summary>
    /// Lấy giá nhập gần nhất của một hàng hóa, nhóm theo nhà cung cấp.
    /// Dùng để gợi ý giá khi thêm sản phẩm vào đơn nhập mới.
    /// </summary>
    Task<IList<RecentPurchasePriceAppDto>> GetRecentPurchasePricesAsync(Guid productId);
}

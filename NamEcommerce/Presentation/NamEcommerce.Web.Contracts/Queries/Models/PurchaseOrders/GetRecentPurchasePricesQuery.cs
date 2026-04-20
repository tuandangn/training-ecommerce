using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

/// <summary>
/// Query lấy giá nhập gần nhất của một sản phẩm theo từng nhà cung cấp.
/// Dùng để gợi ý giá khi tạo đơn nhập hàng mới.
/// </summary>
[Serializable]
public sealed class GetRecentPurchasePricesQuery : IRequest<IList<RecentPurchasePriceModel>>
{
    public required Guid ProductId { get; init; }
}

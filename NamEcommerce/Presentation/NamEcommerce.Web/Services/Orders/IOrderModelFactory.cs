using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Services.Orders;

public interface IOrderModelFactory
{
    Task<OrderListModel> PrepareOrderListModel(OrderListSearchModel searchModel);
    Task<CreateOrderModel> PrepareCreateOrderModel(CreateOrderModel? model = null);
    Task<OrderDetailsModel?> PrepareOrderDetailsModel(Guid orderId);
}

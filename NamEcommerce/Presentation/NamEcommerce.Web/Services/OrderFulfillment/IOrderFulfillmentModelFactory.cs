using NamEcommerce.Web.Models.OrderFulfillment;

namespace NamEcommerce.Web.Services.OrderFulfillment;

public interface IOrderFulfillmentModelFactory
{
    Task<OrderFulfillmentBoardSearchModel> PrepareBoardModelAsync(OrderFulfillmentBoardSearchModel searchModel);
    Task<OrderFulfillmentSchedulePanelModel> PrepareSchedulePanelModelAsync(Guid orderId);
}

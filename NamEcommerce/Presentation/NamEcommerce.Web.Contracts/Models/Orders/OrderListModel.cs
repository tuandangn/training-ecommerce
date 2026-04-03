namespace NamEcommerce.Web.Contracts.Models.Orders;

public sealed class OrderListModel
{
    public IList<OrderListItemModel> Items { get; set; } = new List<OrderListItemModel>();
}

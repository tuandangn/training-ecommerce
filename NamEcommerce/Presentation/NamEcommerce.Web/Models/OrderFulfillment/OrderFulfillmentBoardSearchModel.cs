using NamEcommerce.Web.Contracts.Models.Orders;

namespace NamEcommerce.Web.Models.OrderFulfillment;

[Serializable]
public sealed record OrderFulfillmentBoardSearchModel
{
    public string? Range { get; set; }
    public DateTime? Date { get; set; }
    public string? Keywords { get; set; }
    public string? Risk { get; set; }
    public bool IncludeInactive { get; set; }
    public OrderFulfillmentBoardModel? Board { get; set; }
}

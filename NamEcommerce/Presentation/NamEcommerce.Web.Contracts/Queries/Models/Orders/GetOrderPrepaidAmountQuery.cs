namespace NamEcommerce.Web.Contracts.Queries.Models.Orders;

[Serializable]
public sealed record GetOrderPrepaidAmountQuery : IRequest<decimal>
{
    public Guid OrderId { get; set; }
}

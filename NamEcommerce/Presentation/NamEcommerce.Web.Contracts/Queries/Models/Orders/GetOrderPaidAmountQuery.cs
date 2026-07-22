namespace NamEcommerce.Web.Contracts.Queries.Models.Orders;

[Serializable]
public sealed class GetOrderPaidAmountQuery : IRequest<decimal>
{
    public Guid OrderId { get; init; }
}

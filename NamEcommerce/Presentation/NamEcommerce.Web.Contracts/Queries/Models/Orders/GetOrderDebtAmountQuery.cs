namespace NamEcommerce.Web.Contracts.Queries.Models.Orders;

[Serializable]
public sealed class GetOrderDebtAmountQuery : IRequest<decimal>
{
    public Guid OrderId { get; init; }
}

using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

[Serializable]
public sealed class GetShortageAggregationQuery : IRequest<ShortageAggregationModel>
{
    public Guid? OrderId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public Guid? ProductId { get; init; }
    public bool IncludeSalesOrderScope { get; init; }
}


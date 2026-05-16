using MediatR;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Contracts.Queries.Models.Inventory;

[Serializable]
public sealed class GetOrderShortageInfoQuery : IRequest<ShortageInfoModel>
{
    public Guid OrderId { get; init; }
}

[Serializable]
public sealed class GetDeliveryNoteShortageInfoQuery : IRequest<ShortageInfoModel>
{
    public Guid DeliveryNoteId { get; init; }
    public bool IncludeSalesOrderScope { get; init; }
}


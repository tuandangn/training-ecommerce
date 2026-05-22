using MediatR;

namespace NamEcommerce.Web.Contracts.Queries.Models.Returns;

[Serializable]
public sealed record ReturnedQuantitySummary(decimal ConfirmedQuantity, decimal PendingQuantity);

[Serializable]
public sealed class GetReturnedQuantitiesByDeliveryNoteQuery : IRequest<IReadOnlyDictionary<Guid, ReturnedQuantitySummary>>
{
    public required Guid DeliveryNoteId { get; init; }
}

[Serializable]
public sealed class GetReturnedQuantitiesByGoodsReceiptQuery : IRequest<IReadOnlyDictionary<Guid, ReturnedQuantitySummary>>
{
    public required Guid GoodsReceiptId { get; init; }
}

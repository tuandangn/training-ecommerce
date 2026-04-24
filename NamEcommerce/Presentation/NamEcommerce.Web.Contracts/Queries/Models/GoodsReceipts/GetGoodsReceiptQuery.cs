using MediatR;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;

namespace NamEcommerce.Web.Contracts.Queries.Models.GoodsReceipts;

[Serializable]
public sealed class GetGoodsReceiptQuery : IRequest<GoodsReceiptModel?>
{
    public required Guid Id { get; init; }
}

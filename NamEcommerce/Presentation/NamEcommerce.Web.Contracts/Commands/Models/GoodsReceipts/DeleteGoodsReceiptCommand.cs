using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;

[Serializable]
public sealed class DeleteGoodsReceiptCommand : IRequest<CommonActionResultModel>
{
    public required Guid Id { get; init; }
}

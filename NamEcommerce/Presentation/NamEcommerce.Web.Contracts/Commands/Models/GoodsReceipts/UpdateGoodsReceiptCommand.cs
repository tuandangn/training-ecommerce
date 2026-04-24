using MediatR;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;

namespace NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;

[Serializable]
public sealed class UpdateGoodsReceiptCommand : IRequest<UpdateGoodsReceiptResultModel>
{
    public required Guid Id { get; init; }
    public required DateTime CreatedOn { get; init; }
    public string? TruckDriverName { get; init; }
    public string? TruckNumberSerial { get; init; }
    public required IList<Guid> PictureIds { get; init; }
    public string? Note { get; init; }
}

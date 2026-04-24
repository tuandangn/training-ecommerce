using MediatR;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;

namespace NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;

[Serializable]
public sealed class CreateGoodsReceiptCommand : IRequest<CreateGoodsReceiptResultModel>
{
    public required DateTime CreatedOn { get; init; }
    public string? TruckDriverName { get; init; }
    public string? TruckNumberSerial { get; init; }
    public required IList<Guid> PictureIds { get; init; }
    public string? Note { get; init; }
    public IList<CreateGoodsReceiptItemCommand> Items { get; init; } = [];
}

[Serializable]
public sealed class CreateGoodsReceiptItemCommand
{
    public required Guid ProductId { get; init; }
    public Guid? WarehouseId { get; init; }
    public decimal Quantity { get; init; }
    public decimal? UnitCost { get; init; }
}

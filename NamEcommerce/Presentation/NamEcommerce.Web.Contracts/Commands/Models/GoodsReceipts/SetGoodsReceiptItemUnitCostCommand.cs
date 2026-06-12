using NamEcommerce.Web.Contracts.Models.GoodsReceipts;

namespace NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;

[Serializable]
public sealed class SetGoodsReceiptItemUnitCostCommand : ICommand<SetGoodsReceiptItemUnitCostResultModel>
{
    public required Guid GoodsReceiptId { get; init; }
    public required Guid GoodsReceiptItemId { get; init; }
    public required decimal UnitCost { get; init; }
}

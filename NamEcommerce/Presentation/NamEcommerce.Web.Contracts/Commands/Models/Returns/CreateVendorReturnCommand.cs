using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Commands.Models.Returns;

[Serializable]
public sealed class CreateVendorReturnCommand : IRequest<CreateVendorReturnResultModel>
{
    public required Guid VendorId { get; init; }
    public Guid? GoodsReceiptId { get; init; }
    public Guid? WarehouseId { get; init; }
    public string? Note { get; init; }
    public decimal AdditionalCost { get; init; } = 0;
    public IList<CreateVendorReturnItemCommand> Items { get; init; } = [];
}

[Serializable]
public sealed class CreateVendorReturnItemCommand
{
    public required Guid ProductId { get; init; }
    public Guid? GoodsReceiptItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public int QuantityDecimalPlaces { get; init; }
    public required decimal AcceptedQuantity { get; init; }

    /// <summary>Giá vốn gốc (tham chiếu) — null nếu tạo tự do.</summary>
    public decimal? OriginalUnitCost { get; init; }

    /// <summary>Giá NCC hoàn trả thực tế.</summary>
    public required decimal ReturnUnitCost { get; init; }
}

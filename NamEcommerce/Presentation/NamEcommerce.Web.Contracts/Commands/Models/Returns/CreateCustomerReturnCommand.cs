using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Commands.Models.Returns;

[Serializable]
public sealed class CreateCustomerReturnCommand : IRequest<CreateCustomerReturnResultModel>
{
    /// <summary>Phiếu xuất kho nguồn — null = tạo tự do (cần CustomerId).</summary>
    public Guid? DeliveryNoteId { get; init; }

    /// <summary>Bắt buộc khi DeliveryNoteId = null.</summary>
    public Guid? CustomerId { get; init; }

    public required Guid WarehouseId { get; init; }
    public string? Note { get; init; }

    /// <summary>Chi phí phát sinh (vận chuyển, bồi thường...) — giảm vào khoản hoàn khách.</summary>
    public decimal AdditionalCost { get; init; } = 0;

    public IList<CreateCustomerReturnItemCommand> Items { get; init; } = [];
}

[Serializable]
public sealed class CreateCustomerReturnItemCommand
{
    public required Guid ProductId { get; init; }
    public Guid? DeliveryNoteItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }

    /// <summary>Giá bán gốc (tham chiếu) — null nếu tạo tự do.</summary>
    public decimal? OriginalUnitPrice { get; init; }

    /// <summary>Giá hoàn trả thực tế.</summary>
    public required decimal ReturnUnitPrice { get; init; }
}

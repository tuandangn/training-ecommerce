using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Queries.Models.Returns;

[Serializable]
public sealed class GetReturnableItemsByCustomerQuery : IRequest<List<ReturnableItemModel>>
{
    public required Guid CustomerId { get; init; }

    /// <summary>Bỏ qua phiếu trả hàng này khi tính số lượng đã giữ/trả (dùng khi đang edit).</summary>
    public Guid? ExcludeReturnId { get; init; }
}

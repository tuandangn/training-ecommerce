using MediatR;

namespace NamEcommerce.Web.Contracts.Queries.Models.Returns;

/// <summary>
/// Trả về danh sách WarehouseId còn đủ tồn (QuantityAvailable) để đáp ứng TOÀN BỘ items đang trả.
/// </summary>
[Serializable]
public sealed class GetValidWarehousesForReturnQuery : IRequest<List<Guid>>
{
    public IList<ReturnItemQuantityModel> Items { get; init; } = [];
}

[Serializable]
public sealed class ReturnItemQuantityModel
{
    public required Guid ProductId { get; init; }
    public required decimal RequiredQty { get; init; }
}

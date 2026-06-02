using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Returns;

/// <summary>
/// Lấy danh sách items có thể trả của một phiếu nhập kho — bao gồm số lượng đã trả.
/// </summary>
public sealed class GetGoodsReceiptItemsForReturnHandler
    : IRequestHandler<GetGoodsReceiptItemsForReturnQuery, List<ReturnableItemModel>>
{
    private readonly IVendorReturnAppService _vendorReturnAppService;

    public GetGoodsReceiptItemsForReturnHandler(IVendorReturnAppService vendorReturnAppService)
    {
        _vendorReturnAppService = vendorReturnAppService;
    }

    public async Task<List<ReturnableItemModel>> Handle(
        GetGoodsReceiptItemsForReturnQuery request, CancellationToken cancellationToken)
    {
        var dtos = await _vendorReturnAppService
            .GetGoodsReceiptItemsForReturnAsync(request.GoodsReceiptId, request.ExcludeReturnId)
            .ConfigureAwait(false);

        return dtos.Select(d => new ReturnableItemModel
        {
            ProductId = d.ProductId,
            ProductName = d.ProductName,
            Unit = d.Unit,
            OriginalQty = d.OriginalQty,
            AlreadyReturnedQty = d.AlreadyReturnedQty,
            UnitPrice = d.UnitPrice,
            SourceItemId = d.SourceItemId,
            QuantityDecimalPlaces = d.QuantityDecimalPlaces
        }).ToList();
    }
}

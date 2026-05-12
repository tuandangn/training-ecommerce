using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Returns;

/// <summary>
/// Lấy danh sách phiếu nhập kho (FromVendor) của NCC — dùng cho AJAX picker.
/// </summary>
public sealed class GetGoodsReceiptsByVendorHandler
    : IRequestHandler<GetGoodsReceiptsByVendorQuery, List<GoodsReceiptPickerModel>>
{
    private readonly IVendorReturnAppService _vendorReturnAppService;

    public GetGoodsReceiptsByVendorHandler(IVendorReturnAppService vendorReturnAppService)
    {
        _vendorReturnAppService = vendorReturnAppService;
    }

    public async Task<List<GoodsReceiptPickerModel>> Handle(
        GetGoodsReceiptsByVendorQuery request, CancellationToken cancellationToken)
    {
        var dtos = await _vendorReturnAppService
            .GetGoodsReceiptsByVendorAsync(request.VendorId, request.PurchaseOrderId)
            .ConfigureAwait(false);

        return dtos.Select(d => new GoodsReceiptPickerModel
        {
            Id = d.Id,
            Code = d.Code,
            Label = d.Label,
            ReceivedOn = DateTimeHelper.ToLocalTime(d.ReceivedOnUtc),
            PurchaseOrderCode = d.PurchaseOrderCode,
            WarehouseIds = d.WarehouseIds,
            WarehouseNames = d.WarehouseNames,
            ItemCount = d.ItemCount,
            TotalQuantity = d.TotalQuantity,
            TotalValue = d.TotalValue,
            IsPendingCosting = d.IsPendingCosting,
            IsFullyReturned = d.IsFullyReturned
        }).ToList();
    }
}

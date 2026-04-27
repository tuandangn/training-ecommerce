using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Queries.Models.GoodsReceipts;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.GoodsReceipts;

public sealed class GetGoodsReceiptListHandler : IRequestHandler<GetGoodsReceiptListQuery, GoodsReceiptListModel>
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;
    private readonly IUnitMeasurementAppService _unitMeasurementAppService;

    public GetGoodsReceiptListHandler(IGoodsReceiptAppService goodsReceiptAppService, IUnitMeasurementAppService unitMeasurementAppService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
        _unitMeasurementAppService = unitMeasurementAppService;
    }

    public async Task<GoodsReceiptListModel> Handle(GetGoodsReceiptListQuery request, CancellationToken cancellationToken)
    {
        var result = await _goodsReceiptAppService.GetGoodsReceiptsAsync(
            request.PageIndex,
            request.PageSize,
            request.Keywords,
            DateTimeHelper.ToUniversalTime(request.FromDate),
            DateTimeHelper.ToUniversalTime(request.ToDate)
        ).ConfigureAwait(false);

        var items = result.Select(dto => new GoodsReceiptListModel.ItemModel(dto.Id)
        {
            ReceivedOn = dto.ReceivedOnUtc.ToLocalTime(),
            TruckDriverName = dto.TruckDriverName,
            TruckNumberSerial = dto.TruckNumberSerial,
            IsPendingCosting = dto.IsPendingCosting,
            ItemCount = dto.Items.Count,
            Items = dto.Items.Select(i => new GoodsReceiptListModel.ItemSummary
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName ?? "",
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            }).ToList(),
            TotalValue = dto.IsPendingCosting
                ? null
                : dto.Items.Sum(i => i.Quantity * (i.UnitCost ?? 0))
        }).ToList();

        return new GoodsReceiptListModel
        {
            Keywords = request.Keywords,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Data = PagedDataModel.Create(items, result.Pagination.PageIndex, result.Pagination.PageSize, result.Pagination.TotalCount)
        };
    }
}

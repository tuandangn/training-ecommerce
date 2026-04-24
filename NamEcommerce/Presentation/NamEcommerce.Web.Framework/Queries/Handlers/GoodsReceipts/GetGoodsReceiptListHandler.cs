using MediatR;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Queries.Models.GoodsReceipts;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.GoodsReceipts;

public sealed class GetGoodsReceiptListHandler : IRequestHandler<GetGoodsReceiptListQuery, GoodsReceiptListModel>
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;

    public GetGoodsReceiptListHandler(IGoodsReceiptAppService goodsReceiptAppService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
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
            CreatedOn = dto.CreatedOnUtc.ToLocalTime(),
            TruckDriverName = dto.TruckDriverName,
            TruckNumberSerial = dto.TruckNumberSerial,
            IsPendingCosting = dto.IsPendingCosting,
            ItemCount = dto.Items.Count
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

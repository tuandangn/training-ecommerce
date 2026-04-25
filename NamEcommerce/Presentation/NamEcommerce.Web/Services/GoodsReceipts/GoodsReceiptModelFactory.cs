using MediatR;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Models.GoodsReceipts;

namespace NamEcommerce.Web.Services.GoodsReceipts;

public sealed class GoodsReceiptModelFactory : IGoodsReceiptModelFactory
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;

    public GoodsReceiptModelFactory(AppConfig appConfig, IMediator mediator)
    {
        _appConfig = appConfig;
        _mediator = mediator;
    }

    public async Task<CreateGoodsReceiptModel> PrepareCreateGoodsReceiptModel(CreateGoodsReceiptModel? model = null)
    {
        var warehouses = await _mediator.Send(new GetWarehouseOptionListQuery()).ConfigureAwait(false);

        model ??= new CreateGoodsReceiptModel
        {
            CreatedOn = DateTime.Now,
        };

        model.AvailableWarehouses = warehouses;

        var itemsNeedInfo = model.Items
            .Where(i => i.ProductId.HasValue)
            .ToList();
        if (itemsNeedInfo.Count > 0)
        {
            var ids = itemsNeedInfo.Select(i => i.ProductId!.Value).Distinct();
            var products = await _mediator.Send(new GetProductsByIdsForOrderQuery { Ids = ids })
                .ConfigureAwait(false);

            var infoMap = products.ToDictionary(p => p.Id, p => (p.Name, p.PictureUrl));

            foreach (var item in itemsNeedInfo)
            {
                if (item.ProductId.HasValue && infoMap.TryGetValue(item.ProductId.Value, out var info))
                {
                    item.ProductDisplayPicture = info.PictureUrl;
                    item.ProductDisplayName = info.Name;
                }
            }
        }

        if (model.WarehouseId.HasValue)
        {
            foreach(var item in model.Items)
            {
                item.WarehouseId = model.WarehouseId;
            }
        }

        return model;
    }

    public async Task<GoodsReceiptDetailsModel?> PrepareGoodsReceiptDetailsModel(Guid id)
    {
        var goodsReceipt = await _mediator.Send(new GetGoodsReceiptQuery { Id = id }).ConfigureAwait(false);
        if (goodsReceipt is null)
            return null;

        var model = new GoodsReceiptDetailsModel
        {
            Id = goodsReceipt.Id,
            CreatedOn = goodsReceipt.CreatedOn,
            TruckDriverName = goodsReceipt.TruckDriverName,
            TruckNumberSerial = goodsReceipt.TruckNumberSerial,
            PictureIds = goodsReceipt.PictureIds,
            Note = goodsReceipt.Note,
            IsPendingCosting = goodsReceipt.IsPendingCosting
        };

        foreach (var item in goodsReceipt.Items)
        {
            model.Items.Add(new GoodsReceiptDetailsModel.ItemModel(item.Id)
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                WarehouseId = item.WarehouseId,
                WarehouseName = item.WarehouseName,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                IsPendingCosting = item.IsPendingCosting
            });
        }

        return model;
    }

    public async Task<GoodsReceiptListModel> PrepareGoodsReceiptListModel(GoodsReceiptListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;

        return await _mediator.Send(new GetGoodsReceiptListQuery
        {
            Keywords = searchModel?.Keywords,
            FromDate = searchModel?.FromDate,
            ToDate = searchModel?.ToDate,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        }).ConfigureAwait(false);
    }
}

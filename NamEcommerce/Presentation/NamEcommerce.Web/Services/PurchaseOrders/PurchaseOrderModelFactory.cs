using MediatR;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Customers;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Models.PurchaseOrders;

namespace NamEcommerce.Web.Services.PurchaseOrders;

public sealed class PurchaseOrderModelFactory : IPurchaseOrderModelFactory
{
    private readonly IMediator _mediator;
    private readonly AppConfig _appConfig;

    public PurchaseOrderModelFactory(IMediator mediator, AppConfig appConfig)
    {
        _mediator = mediator;
        _appConfig = appConfig;
    }

    public async Task<CreatePurchaseOrderModel> PrepareCreatePurchaseOrderModel(CreatePurchaseOrderModel? oldModel = null)
    {
        var vendorOptions = await _mediator.Send(new GetVendorOptionListQuery()).ConfigureAwait(false);
        var warehouseOptions = await _mediator.Send(new GetWarehouseOptionListQuery()).ConfigureAwait(false);
        var model = oldModel ?? new CreatePurchaseOrderModel
        {
            AvailableVendors = vendorOptions,
            AvailableWarehouses = warehouseOptions,
        };
        if (oldModel is not null)
        {
            model.AvailableVendors = vendorOptions;
            model.AvailableWarehouses = warehouseOptions;
        }

        if (model.VendorId.HasValue)
        {
            var vendor = await _mediator.Send(new GetVendorQuery { Id = model.VendorId.Value }).ConfigureAwait(false);
            if (vendor is null)
                model.VendorId = null;
            else
            {
                model.VendorName = vendor.Name;
                model.VendorPhone = vendor.PhoneNumber;
                model.VendorAddress = vendor.Address;
            }
        }

        if (model.Items.Count > 0)
        {
            var productIds = model.Items.Select(i => i.ProductId).OfType<Guid>().ToList();
            if (productIds.Count > 0)
            {
                var products = await _mediator.Send(new GetProductsByIdsForOrderQuery
                {
                    Ids = productIds
                }).ConfigureAwait(false);

                model.Items = model.Items.Where(i => products.Any(p => p.Id == i.ProductId)).ToList();

                foreach (var item in model.Items)
                {
                    var product = products.First(p => p.Id == item.ProductId);
                    item.ProductDisplayName = product.Name;
                    item.ProductDisplayPicture = product.PictureUrl;
                }
            }
        }

        return model;
    }

    public async Task<PurchaseOrderListModel> PreparePurchaseOrderListModel(PurchaseOrderListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;
        if (_appConfig.PageSizeOptions.Contains(pageSize)) pageSize = _appConfig.DefaultPageSize;

        var model = await _mediator.Send(new GetPurchaseOrderListQuery
        {
            Keywords = searchModel?.Keywords,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return model;
    }

    public async Task<PurchaseOrderDetailsModel?> PreparePurchaseOrderDetailsModel(Guid id)
    {
        var purchaseOrderInfo = await _mediator.Send(new GetPurchaseOrderQuery { Id = id }).ConfigureAwait(false);
        if (purchaseOrderInfo == null)
            return null;

        var availableProducts = await _mediator.Send(new GetProductOptionListQuery()).ConfigureAwait(false);
        var availableWarehouses = await _mediator.Send(new GetWarehouseOptionListQuery()).ConfigureAwait(false);
        var availableVendors = await _mediator.Send(new GetVendorOptionListQuery()).ConfigureAwait(false);

        var model = new PurchaseOrderDetailsModel
        {
            Info = purchaseOrderInfo,
            AvailableWarehouses = availableWarehouses
        };
        model.CanModifyInfo = purchaseOrderInfo.Status != (int) PurchaseOrderStatus.Submitted
            && purchaseOrderInfo.Status != (int) PurchaseOrderStatus.Completed 
            && purchaseOrderInfo.Status != (int) PurchaseOrderStatus.Cancelled;
        if (model.CanModifyInfo)
        {
            model.ModifyInfo = new EditPurchaseOrderModel
            {
                Id = purchaseOrderInfo.Id,
                VendorId = purchaseOrderInfo.VendorId,
                VendorName = purchaseOrderInfo.VendorName,
                Note = purchaseOrderInfo.Note,
                AvailableVendors = availableVendors,
                ExpectedDeliveryDate = purchaseOrderInfo.ExpectedDeliveryDate,
                ShippingAmount = purchaseOrderInfo.ShippingAmount,
                TaxAmount = purchaseOrderInfo.TaxAmount,
                CanChangeVendor = purchaseOrderInfo.Status == (int) PurchaseOrderStatus.Draft,
                CanChangeDate = purchaseOrderInfo.Status == (int) PurchaseOrderStatus.Draft,
                CanChangeFees = purchaseOrderInfo.Items.Count > 0,
                TotalAmount = purchaseOrderInfo.TotalAmount,
                CreatedOn = purchaseOrderInfo.CreatedOn
            };
        }
        if (model.Info.CanAddItems)
        {
            model.AddItemModel = new AddPurchaseOrderItemModel
            {
                PurchaseOrderId = purchaseOrderInfo.Id,
                AvailableProducts = availableProducts
            };
        }
        else if (model.Info.CanReceiveGoods)
        {
            foreach (var item in model.Info.Items)
            {
                if (item.RemainingQuantity <= 0)
                    continue;

                model.ReceiveItemModels.Add(new ReceivePurchaseOrderItemModel
                {
                    PurchaseOrderItemId = item.Id,
                    PurchaseOrderId = purchaseOrderInfo.Id,
                    RemainingQuantity = item.RemainingQuantity,
                    WarehouseRequired = item.TrackInventory,
                    WarehouseId = purchaseOrderInfo.WarehouseId,
                });
            }
        }

        return model;
    }
}

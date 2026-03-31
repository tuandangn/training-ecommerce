using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.PurchaseOrders;

public sealed class GetPurchaseOrderListHandler : IRequestHandler<GetPurchaseOrderListQuery, PurchaseOrderListModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;
    private readonly IVendorAppService _vendorAppService;
    private readonly IWarehouseAppService _warehouseAppService;

    public GetPurchaseOrderListHandler(IPurchaseOrderAppService appService, IVendorAppService vendorAppService, IWarehouseAppService warehouseAppService)
    {
        _purchaseOrderAppService = appService;
        _vendorAppService = vendorAppService;
        _warehouseAppService = warehouseAppService;
    }

    public async Task<PurchaseOrderListModel> Handle(GetPurchaseOrderListQuery request, CancellationToken cancellationToken)
    {
        var result = await _purchaseOrderAppService.GetPurchaseOrdersAsync(request.Keywords, request.PageIndex, request.PageSize).ConfigureAwait(false);

        var purchaseOrders = new List<PurchaseOrderListModel.ItemModel>();
        foreach (var itemInfo in result)
        {
            var purchaseOrderModel = new PurchaseOrderListModel.ItemModel(itemInfo.Id)
            {
                Code = itemInfo.Code,
                Status = itemInfo.Status,
                TotalAmount = itemInfo.TotalAmount,
                ExpectedDeliveryDate = itemInfo.ExpectedDeliveryDateUtc?.ToLocalTime(),
                CreatedOn = itemInfo.CreatedOnUtc.ToLocalTime()
            };

            if (itemInfo.VendorId.HasValue)
            {
                var vendor = await _vendorAppService.GetVendorByIdAsync(itemInfo.VendorId.Value).ConfigureAwait(false);
                purchaseOrderModel.VendorName = vendor?.Name;
            }

            if (itemInfo.WarehouseId.HasValue)
            {
                var warehouse = await _warehouseAppService.GetWarehouseByIdAsync(itemInfo.WarehouseId.Value).ConfigureAwait(false);
                purchaseOrderModel.WarehouseName = warehouse?.Name;
            }

            purchaseOrders.Add(purchaseOrderModel);
        }

        return new PurchaseOrderListModel
        {
            Keywords = request.Keywords,
            Data = PagedDataModel.Create(purchaseOrders, result.Pagination.PageIndex, result.Pagination.PageSize, result.Pagination.TotalCount)
        };
    }
}

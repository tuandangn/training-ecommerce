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
        var pagedData = await _purchaseOrderAppService.GetPurchaseOrdersAsync(request.PageIndex, request.PageSize, request.Keywords, request.Status).ConfigureAwait(false);

        var purchaseOrders = new List<PurchaseOrderListModel.ItemModel>();
        foreach (var purchaseOrder in pagedData)
        {
            var purchaseOrderModel = new PurchaseOrderListModel.ItemModel(purchaseOrder.Id)
            {
                Code = purchaseOrder.Code,
                PlacedOn = purchaseOrder.PlacedOnUtc.ToLocalTime(),
                Status = purchaseOrder.Status,
                TotalAmount = purchaseOrder.TotalAmount,
                ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDateUtc?.ToLocalTime(),
                CreatedOn = purchaseOrder.CreatedOnUtc.ToLocalTime()
            };

            var vendor = await _vendorAppService.GetVendorByIdAsync(purchaseOrder.VendorId).ConfigureAwait(false);
            purchaseOrderModel.VendorName = vendor?.Name;
            purchaseOrderModel.VendorPhone = vendor?.PhoneNumber;

            if (purchaseOrder.WarehouseId.HasValue)
            {
                var warehouse = await _warehouseAppService.GetWarehouseByIdAsync(purchaseOrder.WarehouseId.Value).ConfigureAwait(false);
                purchaseOrderModel.WarehouseName = warehouse?.Name;
            }

            purchaseOrders.Add(purchaseOrderModel);
        }

        return new PurchaseOrderListModel
        {
            Keywords = request.Keywords,
            Status = request.Status,
            Data = PagedDataModel.Create(purchaseOrders, pagedData.Pagination.PageIndex, pagedData.Pagination.PageSize, pagedData.Pagination.TotalCount)
        };
    }
}

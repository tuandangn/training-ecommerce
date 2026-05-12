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
    private readonly IProductAppService _productAppService;

    public GetPurchaseOrderListHandler(IPurchaseOrderAppService appService, IVendorAppService vendorAppService,
        IWarehouseAppService warehouseAppService, IProductAppService productAppService)
    {
        _purchaseOrderAppService = appService;
        _vendorAppService = vendorAppService;
        _warehouseAppService = warehouseAppService;
        _productAppService = productAppService;
    }

    public async Task<PurchaseOrderListModel> Handle(GetPurchaseOrderListQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await _purchaseOrderAppService.GetPurchaseOrdersAsync(request.PageIndex, request.PageSize, request.Keywords, request.Status).ConfigureAwait(false);

        // Batch-fetch product names for all items across the page (one round-trip thay vì N).
        var allProductIds = pagedData.SelectMany(po => po.Items.Select(item => item.ProductId)).Distinct().ToList();
        var productMap = allProductIds.Count > 0
            ? (await _productAppService.GetProductsByIdsAsync(allProductIds).ConfigureAwait(false))
                .ToDictionary(p => p.Id, p => p.Name)
            : [];

        var purchaseOrders = new List<PurchaseOrderListModel.PurchaseModel>();
        foreach (var purchaseOrder in pagedData)
        {
            var itemSummaries = purchaseOrder.Items.Select(item => new ItemSummaryModel
            {
                ProductName = productMap.TryGetValue(item.ProductId, out var name) ? name : "Hàng hóa đã xóa",
                QuantityOrdered = item.QuantityOrdered,
                QuantityReceived = item.QuantityReceived
            }).ToList();

            var purchaseOrderModel = new PurchaseOrderListModel.PurchaseModel(purchaseOrder.Id)
            {
                Code = purchaseOrder.Code,
                PlacedOn = purchaseOrder.PlacedOnUtc.ToLocalTime(),
                Status = purchaseOrder.Status,
                TotalAmount = purchaseOrder.TotalAmount,
                ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDateUtc?.ToLocalTime(),
                CreatedOn = purchaseOrder.CreatedOnUtc.ToLocalTime(),
                Items = itemSummaries,
                TotalOrdered = itemSummaries.Sum(i => i.QuantityOrdered),
                TotalReceived = itemSummaries.Sum(i => i.QuantityReceived)
            };

            var vendor = await _vendorAppService.GetVendorByIdAsync(purchaseOrder.VendorId).ConfigureAwait(false);
            purchaseOrderModel.VendorId = purchaseOrder.VendorId;
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

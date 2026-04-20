using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.PurchaseOrders;

public sealed class GetPurchaseOrderHandler : IRequestHandler<GetPurchaseOrderQuery, PurchaseOrderModel?>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;
    private readonly IVendorAppService _vendorAppService;
    private readonly IWarehouseAppService _warehouseAppService;
    private readonly IProductAppService _productAppService;

    public GetPurchaseOrderHandler(IPurchaseOrderAppService appService, IVendorAppService vendorAppService,
        IWarehouseAppService warehouseAppService, IProductAppService productAppService)
    {
        _purchaseOrderAppService = appService;
        _vendorAppService = vendorAppService;
        _warehouseAppService = warehouseAppService;
        _productAppService = productAppService;
    }

    public async Task<PurchaseOrderModel?> Handle(GetPurchaseOrderQuery request, CancellationToken cancellationToken)
    {
        var purchaseOrder = await _purchaseOrderAppService.GetPurchaseOrderByIdAsync(request.Id).ConfigureAwait(false);
        if (purchaseOrder == null) return null;

        var model = new PurchaseOrderModel
        {
            Id = purchaseOrder.Id,
            Code = purchaseOrder.Code,
            VendorId = purchaseOrder.VendorId,
            WarehouseId = purchaseOrder.WarehouseId,
            Status = purchaseOrder.Status,
            Note = purchaseOrder.Note,
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDateUtc?.ToLocalTime(),
            ShippingAmount = purchaseOrder.ShippingAmount,
            TaxAmount = purchaseOrder.TaxAmount,
            TotalAmount = purchaseOrder.TotalAmount,
            CreatedOn =  purchaseOrder.CreatedOnUtc.ToLocalTime(),
            CanAddItems = purchaseOrder.CanAddItems,
            CanReceiveGoods = purchaseOrder.CanReceiveGoods
        };

        foreach (var item in purchaseOrder.Items)
        {
            var itemModel = new PurchaseOrderModel.ItemModel(item.Id)
            {
                ProductId = item.ProductId,
                Note = item.Note,
                QuantityOrdered = item.QuantityOrdered,
                QuantityReceived = item.QuantityReceived,
                RemainingQuantity = item.RemainingQuantity,
                UnitCost = item.UnitCost,
                TotalCost = item.TotalCost
            };
            var product = await _productAppService.GetProductByIdAsync(item.ProductId).ConfigureAwait(false);
            itemModel.ProductName = product?.Name ?? string.Empty;
            itemModel.CurrentUnitPrice = product?.UnitPrice ?? 0m;

            model.Items.Add(itemModel);
        }

        if (model.VendorId.HasValue)
        {
            var vendor = await _vendorAppService.GetVendorByIdAsync(model.VendorId.Value).ConfigureAwait(false);
            model.VendorName = vendor?.Name;
            model.VendorPhone = vendor?.PhoneNumber;
            model.VendorAddress = vendor?.Address;
        }

        if (model.WarehouseId.HasValue)
        {
            var warehouse = await _warehouseAppService.GetWarehouseByIdAsync(model.WarehouseId.Value).ConfigureAwait(false);
            model.WarehouseName = warehouse?.Name;
            model.WarehouseAddress = warehouse?.Address;
        }

        return model;
    }
}

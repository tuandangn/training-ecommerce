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

    public GetPurchaseOrderHandler(IPurchaseOrderAppService appService, IVendorAppService vendorAppService, IWarehouseAppService warehouseAppService)
    {
        _purchaseOrderAppService = appService;
        _vendorAppService = vendorAppService;
        _warehouseAppService = warehouseAppService;
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
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDateUtc,
            TotalAmount = purchaseOrder.TotalAmount,
            CreatedOn = purchaseOrder.CreatedOnUtc.ToLocalTime(),
            Items = purchaseOrder.Items.Select(item => new PurchaseOrderModel.ItemModel(item.Id)
            {
                ProductId = item.ProductId,
                QuantityOrdered = item.QuantityOrdered,
                UnitCost = item.UnitCost,
                QuantityReceived = item.QuantityReceived,
                Note = item.Note
            }).ToList()
        };

        if (model.VendorId.HasValue)
        {
            var vendor = await _vendorAppService.GetVendorByIdAsync(model.VendorId.Value).ConfigureAwait(false);
            model.VendorName = vendor?.Name;
        }

        if (model.WarehouseId.HasValue)
        {
            var warehouse = await _warehouseAppService.GetWarehouseByIdAsync(model.WarehouseId.Value).ConfigureAwait(false);
            model.WarehouseName = warehouse?.Name;
        }

        return model;
    }
}

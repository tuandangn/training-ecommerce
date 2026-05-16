using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.PurchaseOrders;

public sealed class GetPurchaseOrderHandler : IRequestHandler<GetPurchaseOrderQuery, PurchaseOrderModel?>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;
    private readonly IVendorAppService _vendorAppService;
    private readonly IWarehouseAppService _warehouseAppService;
    private readonly IProductAppService _productAppService;
    private readonly IMediator _mediator;

    public GetPurchaseOrderHandler(IPurchaseOrderAppService appService, IVendorAppService vendorAppService,
        IWarehouseAppService warehouseAppService, IProductAppService productAppService, IMediator mediator)
    {
        _purchaseOrderAppService = appService;
        _vendorAppService = vendorAppService;
        _warehouseAppService = warehouseAppService;
        _productAppService = productAppService;
        _mediator = mediator;
    }

    public async Task<PurchaseOrderModel?> Handle(GetPurchaseOrderQuery request, CancellationToken cancellationToken)
    {
        var purchaseOrder = await _purchaseOrderAppService.GetPurchaseOrderByIdAsync(request.Id).ConfigureAwait(false);
        if (purchaseOrder == null) return null;

        var model = new PurchaseOrderModel
        {
            Id = purchaseOrder.Id,
            Code = purchaseOrder.Code,
            PlacedOn = purchaseOrder.PlacedOnUtc.ToLocalTime(),
            VendorId = purchaseOrder.VendorId,
            WarehouseId = purchaseOrder.WarehouseId,
            Status = purchaseOrder.Status,
            Note = purchaseOrder.Note,
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDateUtc?.ToLocalTime(),
            ShippingAmount = purchaseOrder.ShippingAmount,
            TaxAmount = purchaseOrder.TaxAmount,
            AccumulatedShippingAmount = purchaseOrder.AccumulatedShippingAmount,
            AccumulatedTaxAmount = purchaseOrder.AccumulatedTaxAmount,
            TotalAmount = purchaseOrder.TotalAmount,
            CreatedOn = purchaseOrder.CreatedOnUtc.ToLocalTime(),
            CanModifyInfo = purchaseOrder.CanModifyInfo,
            CanAddItems = purchaseOrder.CanAddItems,
            CanReceiveGoods = purchaseOrder.CanReceiveGoods,
            CanChangeDate = purchaseOrder.CanChangeDate,
            CanChangeFees = purchaseOrder.CanChangeFees,
            CanChangeVendor = purchaseOrder.CanChangeVendor
        };

        var products = await _mediator.Send(new GetProductsByIdsForOrderQuery { Ids = purchaseOrder.Items.Select(item => item.ProductId).Distinct() }, cancellationToken).ConfigureAwait(false);
        foreach (var item in purchaseOrder.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product is null) continue;

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
            itemModel.ProductName = product.Name;
            itemModel.CurrentUnitPrice = product.CurrentUnitPrice;
            itemModel.ProductPicture = product.PictureUrl;

            model.Items.Add(itemModel);
        }

        var vendor = await _vendorAppService.GetVendorByIdAsync(model.VendorId).ConfigureAwait(false);
        model.VendorName = vendor?.Name;
        model.VendorPhone = vendor?.PhoneNumber;
        model.VendorAddress = vendor?.Address;

        if (model.WarehouseId.HasValue)
        {
            var warehouse = await _warehouseAppService.GetWarehouseByIdAsync(model.WarehouseId.Value).ConfigureAwait(false);
            model.WarehouseName = warehouse?.Name;
            model.WarehouseAddress = warehouse?.Address;
        }

        return model;
    }
}

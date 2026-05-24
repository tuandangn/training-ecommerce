using MediatR;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Events.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.GoodsReceipts;

namespace NamEcommerce.Application.Services.Events.PurchaseOrders;

public sealed class VendorOversupplyAcceptedHandler(
    IEntityDataReader<PurchaseOrder> purchaseOrderReader,
    IGoodsReceiptManager goodsReceiptManager) : INotificationHandler<VendorOversupplyAccepted>
{
    public async Task Handle(VendorOversupplyAccepted notification, CancellationToken cancellationToken)
    {
        var purchaseOrder = purchaseOrderReader.DataSource
            .FirstOrDefault(order => order.Items.Any(item => item.Id == notification.PurchaseOrderItemId))
            ?? throw new PurchaseOrderItemIsNotFoundException();

        var purchaseOrderItem = purchaseOrder.Items.First(item => item.Id == notification.PurchaseOrderItemId);
        await goodsReceiptManager.CreateFromVendorOversupplyAsync(new CreateGoodsReceiptFromVendorOversupplyDto
        {
            PurchaseOrderId = purchaseOrder.Id,
            PurchaseOrderCode = purchaseOrder.Code,
            VendorId = purchaseOrder.VendorId,
            ProductId = purchaseOrderItem.ProductId,
            WarehouseId = notification.WarehouseId,
            Quantity = notification.OversupplyQuantity,
            UnitCost = notification.UnitCost
        }).ConfigureAwait(false);
    }
}

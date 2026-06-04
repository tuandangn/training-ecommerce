using MediatR;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Dtos.Returns;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.Returns;
using NamEcommerce.Domain.Shared.Services.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Returns;

namespace NamEcommerce.Application.Services.Events.Returns;

/// <summary>
/// Xử lý sự kiện <see cref="CustomerReturnConfirmed"/> — phiếu trả hàng khách vừa được Confirm.
/// <list type="number">
///   <item><description>Tạo <c>GoodsReceipt(SourceType=FromCustomerReturn)</c> để nhận lại hàng vào kho;
///     inventory cost được phục hồi từ allocation gốc nếu đã có.</description></item>
///   <item><description>Set <c>CustomerReturn.GeneratedGoodsReceiptId</c> và giảm <c>CustomerDebt</c>
///     của Order theo FIFO <c>CreatedOnUtc</c> thông qua <see cref="ICustomerReturnManager.FinalizeConfirmAsync"/>.</description></item>
/// </list>
/// Idempotency: nếu <c>GeneratedGoodsReceiptId</c> đã được set, <c>FinalizeConfirmAsync</c> sẽ return sớm.
/// </summary>
public sealed class CustomerReturnConfirmedEventHandler(
    ICustomerReturnManager customerReturnManager,
    IGoodsReceiptManager goodsReceiptManager,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IProductReservationManager productReservationManager) : INotificationHandler<CustomerReturnConfirmed>
{
    private readonly ICustomerReturnManager _customerReturnManager = customerReturnManager;
    private readonly IGoodsReceiptManager _goodsReceiptManager = goodsReceiptManager;
    private readonly IEntityDataReader<DeliveryNote> _deliveryNoteReader = deliveryNoteReader;
    private readonly IProductReservationManager _productReservationManager = productReservationManager;

    public async Task Handle(CustomerReturnConfirmed notification, CancellationToken cancellationToken)
    {
        // Re-fetch để lấy Items đầy đủ — event chỉ mang Id để tránh reference leak qua event boundary.
        var customerReturn = await _customerReturnManager.GetByIdAsync(notification.CustomerReturnId)
            .ConfigureAwait(false);
        if (customerReturn is null) return;

        // Idempotency guard: đã xử lý rồi thì bỏ qua
        if (customerReturn.GeneratedGoodsReceiptId.HasValue) return;

        // 1. Tạo GoodsReceipt nhận lại hàng (SourceType=FromCustomerReturn, cộng tồn + không sinh VendorDebt)
        var goodsReceiptId = await _goodsReceiptManager.CreateFromCustomerReturnAsync(
            new CreateGoodsReceiptFromCustomerReturnDto
            {
                CustomerReturnId = notification.CustomerReturnId,
                WarehouseId = notification.WarehouseId,
                Items = customerReturn.Items.Select(i => new CreateGoodsReceiptFromCustomerReturnItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.AcceptedQuantity
                })
            }).ConfigureAwait(false);

        // 2. Ghi nhận GoodsReceiptId + giảm CustomerDebt FIFO (net = Σ AcceptedTotal - AdditionalCost)
        await ReserveCompensatedQuantityAsync(customerReturn).ConfigureAwait(false);

        var totalReturnAmount = customerReturn.NetRefundAmount;
        await _customerReturnManager.FinalizeConfirmAsync(
            notification.CustomerReturnId, goodsReceiptId, totalReturnAmount).ConfigureAwait(false);
    }

    private async Task ReserveCompensatedQuantityAsync(CustomerReturnDto customerReturn)
    {
        if (!customerReturn.CompensateInNextDelivery)
            return;

        var sourceItemsById = _deliveryNoteReader.DataSource
            .Where(note => note.CustomerId == customerReturn.CustomerId && note.OrderId != Guid.Empty)
            .SelectMany(note => note.Items.Select(item => new
            {
                note.OrderId,
                item.Id,
                item.ProductId,
                item.OrderItemId
            }))
            .ToDictionary(item => item.Id);
        if (sourceItemsById.Count == 0)
            return;

        var quantitiesByOrderProduct = customerReturn.Items
            .Where(item => item.AcceptedQuantity > 0 && item.DeliveryNoteItemId.HasValue)
            .Select(item => new
            {
                ReturnItem = item,
                SourceItem = sourceItemsById.GetValueOrDefault(item.DeliveryNoteItemId!.Value)
            })
            .Where(item => item.SourceItem is not null
                && item.SourceItem.ProductId == item.ReturnItem.ProductId
                && item.SourceItem.OrderItemId != Guid.Empty)
            .GroupBy(item => new { item.SourceItem!.OrderId, item.ReturnItem.ProductId })
            .Select(group => new
            {
                group.Key.OrderId,
                group.Key.ProductId,
                Quantity = group.Sum(item => item.ReturnItem.AcceptedQuantity)
            })
            .ToList();

        foreach (var item in quantitiesByOrderProduct)
        {
            var alreadyReserved = await _productReservationManager.GetReservedByReferenceAsync(
                item.ProductId,
                item.OrderId,
                ProductReservationReason.CustomerReturnCompensation,
                customerReturn.Id).ConfigureAwait(false);

            var quantityToReserve = item.Quantity - alreadyReserved;
            if (quantityToReserve <= 0)
                continue;

            await _productReservationManager.ReserveAsync(
                item.ProductId,
                quantityToReserve,
                item.OrderId,
                ProductReservationReason.CustomerReturnCompensation,
                customerReturn.Id).ConfigureAwait(false);
        }
    }
}

using MediatR;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Events.Returns;
using NamEcommerce.Domain.Shared.Services.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.Returns;

namespace NamEcommerce.Application.Services.Events.Returns;

/// <summary>
/// Xử lý sự kiện <see cref="CustomerReturnConfirmed"/> — phiếu trả hàng khách vừa được Confirm.
/// <list type="number">
///   <item><description>Tạo <c>GoodsReceipt(SourceType=FromCustomerReturn)</c> để nhận lại hàng vào kho;
///     UnitCost = AverageCost tại thời điểm trả (lấy trong GoodsReceiptManager).</description></item>
///   <item><description>Set <c>CustomerReturn.GeneratedGoodsReceiptId</c> và giảm <c>CustomerDebt</c>
///     của Order theo FIFO <c>CreatedOnUtc</c> thông qua <see cref="ICustomerReturnManager.FinalizeConfirmAsync"/>.</description></item>
/// </list>
/// Idempotency: nếu <c>GeneratedGoodsReceiptId</c> đã được set, <c>FinalizeConfirmAsync</c> sẽ return sớm.
/// </summary>
public sealed class CustomerReturnConfirmedEventHandler(
    ICustomerReturnManager customerReturnManager,
    IGoodsReceiptManager goodsReceiptManager) : INotificationHandler<CustomerReturnConfirmed>
{
    private readonly ICustomerReturnManager _customerReturnManager = customerReturnManager;
    private readonly IGoodsReceiptManager _goodsReceiptManager = goodsReceiptManager;

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
                    Quantity = i.AcceptedQuantity,
                    ReturnUnitPrice = i.ReturnUnitPrice
                })
            }).ConfigureAwait(false);

        // 2. Ghi nhận GoodsReceiptId + giảm CustomerDebt FIFO (net = Σ AcceptedTotal - AdditionalCost)
        var totalReturnAmount = customerReturn.NetRefundAmount;
        await _customerReturnManager.FinalizeConfirmAsync(
            notification.CustomerReturnId, goodsReceiptId, totalReturnAmount).ConfigureAwait(false);
    }
}

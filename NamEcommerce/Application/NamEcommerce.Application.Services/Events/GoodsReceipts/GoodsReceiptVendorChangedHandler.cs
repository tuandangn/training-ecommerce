using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Events.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Application.Services.Events.GoodsReceipts;

/// <summary>
/// Handler cho <see cref="GoodsReceiptVendorChanged"/>. Khi vendor của phiếu nhập vừa được
/// gắn / đổi, thử sinh công nợ NCC nếu phiếu đã đủ điều kiện (toàn bộ items đã định giá).
/// Idempotent qua <see cref="IVendorDebtManager.CreateDebtFromGoodsReceiptAsync"/>.
///
/// <para>Tách riêng khỏi <see cref="GoodsReceiptItemUnitCostSetHandler"/> vì 2 trigger khác nhau:
/// SetUnitCost cần tính lại AverageCost rồi mới try debt; SetVendor chỉ cần try debt.</para>
/// </summary>
public sealed class GoodsReceiptVendorChangedHandler : INotificationHandler<GoodsReceiptVendorChanged>
{
    private readonly IEntityDataReader<GoodsReceipt> _goodsReceiptDataReader;
    private readonly IVendorDebtManager _vendorDebtManager;

    public GoodsReceiptVendorChangedHandler(
        IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
        IVendorDebtManager vendorDebtManager)
    {
        _goodsReceiptDataReader = goodsReceiptDataReader;
        _vendorDebtManager = vendorDebtManager;
    }

    public async Task Handle(GoodsReceiptVendorChanged notification, CancellationToken cancellationToken)
    {
        var goodsReceipt = await _goodsReceiptDataReader.GetByIdAsync(notification.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is null) return;

        // Còn item chưa có giá → tổng tiền chưa xác định.
        if (goodsReceipt.IsPendingCosting()) return;
        // Vendor đã bị clear (SetGoodsReceiptVendor với null) → không sinh nợ.
        if (!goodsReceipt.VendorId.HasValue) return;

        var totalAmount = goodsReceipt.Items.Sum(i => i.Quantity * i.UnitCost!.Value);
        if (totalAmount <= 0) return;

        await _vendorDebtManager.CreateDebtFromGoodsReceiptAsync(new CreateVendorDebtFromGoodsReceiptDto
        {
            VendorId = goodsReceipt.VendorId.Value,
            GoodsReceiptId = goodsReceipt.Id,
            TotalAmount = totalAmount,
            CreatedByUserId = goodsReceipt.CreatedByUserId
        }).ConfigureAwait(false);
    }
}

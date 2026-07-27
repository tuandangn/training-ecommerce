using MediatR;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Application.Services.Notifications;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Events.GoodsReceipts;

namespace NamEcommerce.Application.Services.Events.GoodsReceipts;

public sealed class GoodsReceiptSystemNotificationHandler(
    IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
    ISystemNotificationAppService notificationAppService) : INotificationHandler<GoodsReceiptCreated>
{
    public async Task Handle(GoodsReceiptCreated notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            return;

        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(notification.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is null)
            return;

        await notificationAppService
            .CreateAsync(ProcurementSystemNotificationComposer.GoodsReceiptCreated(goodsReceipt.Id, goodsReceipt.Code))
            .ConfigureAwait(false);
    }
}

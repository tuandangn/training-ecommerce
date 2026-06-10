using NamEcommerce.Application.Contracts.Dtos.Notifications;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Notifications;

namespace NamEcommerce.Web.Services.DeliveryNotes;

/// <summary>
/// Phát hiện DN Delivered quá <see cref="ThresholdMinutes"/> phút mà thiếu debt hoặc StockMovementLog → ghi system notification.
/// </summary>
public sealed class DeliveryNoteReconciliationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DeliveryNoteReconciliationHostedService> logger) : BackgroundService
{
    private const int ThresholdMinutes = 10;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim RunLock = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DeliveryNote reconciliation worker failed.");
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!await RunLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var dnReader = scope.ServiceProvider.GetRequiredService<IEntityDataReader<DeliveryNote>>();
            var debtReader = scope.ServiceProvider.GetRequiredService<IEntityDataReader<CustomerDebt>>();
            var stockLogReader = scope.ServiceProvider.GetRequiredService<IEntityDataReader<StockMovementLog>>();
            var notificationService = scope.ServiceProvider.GetRequiredService<ISystemNotificationAppService>();

            var threshold = DateTime.UtcNow.AddMinutes(-ThresholdMinutes);

            var deliveredDns = dnReader.DataSource
                .Where(dn => dn.Status == DeliveryNoteStatus.Delivered
                          && dn.DeliveredOnUtc != null
                          && dn.DeliveredOnUtc < threshold)
                .Select(dn => new
                {
                    dn.Id,
                    dn.Code,
                    dn.AmountToCollect,
                    dn.SourceType,
                    dn.IsDirectShip,
                    dn.DeliveredOnUtc
                })
                .ToList();

            foreach (var dn in deliveredDns)
            {
                // Check 1: missing debt
                if (dn.AmountToCollect > 0 && dn.SourceType != DeliveryNoteSourceType.ToVendorReturn)
                {
                    var hasDebt = debtReader.DataSource.Any(d => d.DeliveryNoteId == dn.Id);
                    if (!hasDebt)
                    {
                        logger.LogWarning("Reconciliation: DN {Code} ({Id}) Delivered but missing CustomerDebt.", dn.Code, dn.Id);
                        await notificationService.CreateAsync(new CreateSystemNotificationAppDto
                        {
                            Type = (int)SystemNotificationType.DeliveryNoteReconciliationAlert,
                            Severity = (int)SystemNotificationSeverity.Warning,
                            Title = "Phiếu xuất thiếu công nợ",
                            Message = $"Phiếu xuất {dn.Code} đã giao nhưng chưa có phiếu công nợ tương ứng (AmountToCollect = {dn.AmountToCollect:N0}).",
                            RequiredPermission = "DeliveryNotes.Manage",
                            RelatedEntityId = dn.Id,
                            RelatedEntityType = "DeliveryNote",
                            ActionUrl = $"/DeliveryNotes/Detail?id={dn.Id}"
                        }).ConfigureAwait(false);
                    }
                }

                // Check 2: direct-ship DNs — stock movement must have been dispatched via handler
                if (dn.IsDirectShip)
                {
                    var hasStockLog = stockLogReader.DataSource.Any(
                        log => log.ReferenceId == dn.Id
                            && log.ReferenceType == StockReferenceType.SalesOrder
                            && log.MovementType == StockMovementType.Outbound);

                    if (!hasStockLog)
                    {
                        logger.LogWarning("Reconciliation: DirectShip DN {Code} ({Id}) missing StockMovementLog.", dn.Code, dn.Id);
                        await notificationService.CreateAsync(new CreateSystemNotificationAppDto
                        {
                            Type = (int)SystemNotificationType.DeliveryNoteReconciliationAlert,
                            Severity = (int)SystemNotificationSeverity.Warning,
                            Title = "Phiếu xuất direct-ship thiếu trừ kho",
                            Message = $"Phiếu xuất {dn.Code} (direct-ship) đã giao nhưng chưa có bút toán trừ kho tương ứng.",
                            RequiredPermission = "DeliveryNotes.Manage",
                            RelatedEntityId = dn.Id,
                            RelatedEntityType = "DeliveryNote",
                            ActionUrl = $"/DeliveryNotes/Detail?id={dn.Id}"
                        }).ConfigureAwait(false);
                    }
                }
            }
        }
        finally
        {
            RunLock.Release();
        }
    }
}

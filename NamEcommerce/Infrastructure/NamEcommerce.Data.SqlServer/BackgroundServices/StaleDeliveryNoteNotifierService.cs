using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

namespace NamEcommerce.Data.SqlServer.BackgroundServices;

public sealed class StaleDeliveryNoteNotifierService(
    IServiceScopeFactory scopeFactory,
    ILogger<StaleDeliveryNoteNotifierService> logger) : BackgroundService
{
    private const int StaleThresholdDays = 7;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken).ConfigureAwait(false);

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await RunAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Stale delivery note notification failed");
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NamEcommerceEfDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-StaleThresholdDays);
        var staleNotes = await db.Set<DeliveryNote>()
            .Where(n =>
                (n.Status == DeliveryNoteStatus.Confirmed || n.Status == DeliveryNoteStatus.Delivering) &&
                n.CreatedOnUtc < cutoff &&
                n.SourceType == DeliveryNoteSourceType.ToCustomer)
            .Select(n => new { n.Id, n.Code, n.CreatedOnUtc })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var note in staleNotes)
        {
            logger.LogWarning(
                "Stale delivery note detected: id={Id} code={Code} created={CreatedOn}. Requires manual handling.",
                note.Id, note.Code, note.CreatedOnUtc);
        }
    }
}

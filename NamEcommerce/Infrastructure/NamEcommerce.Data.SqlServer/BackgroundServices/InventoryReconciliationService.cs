using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NamEcommerce.Data.SqlServer.BackgroundServices;

public sealed class InventoryReconciliationService(
    IServiceScopeFactory scopeFactory,
    ILogger<InventoryReconciliationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once at startup (midnight jobs or admin-triggered); interval is long by design.
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken).ConfigureAwait(false);

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await RunAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Inventory reconciliation failed");
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NamEcommerceEfDbContext>();

        var stocks = await db.Set<InventoryStock>().ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var stock in stocks)
        {
            var movementSum = await db.Set<StockMovementLog>()
                .Where(l => l.ProductId == stock.ProductId && l.WarehouseId == stock.WarehouseId)
                .SumAsync(l =>
                    (l.MovementType == StockMovementType.Inbound ? l.Quantity : 0m) -
                    (l.MovementType == StockMovementType.Outbound || l.MovementType == StockMovementType.Transfer || l.MovementType == StockMovementType.Revert ? l.Quantity : 0m) +
                    (l.MovementType == StockMovementType.Adjustment ? l.Quantity : 0m),
                    cancellationToken).ConfigureAwait(false);

            if (Math.Abs(stock.QuantityOnHand - movementSum) > 0.001m)
            {
                logger.LogWarning(
                    "Inventory mismatch: product={ProductId} warehouse={WarehouseId} stock={OnHand} logs={LogSum}",
                    stock.ProductId, stock.WarehouseId, stock.QuantityOnHand, movementSum);
            }

            var reservedSum = await db.Set<StockReservationEntry>()
                .Where(e => e.ProductId == stock.ProductId && e.WarehouseId == stock.WarehouseId)
                .SumAsync(e => e.QuantityDelta, cancellationToken).ConfigureAwait(false);

            if (Math.Abs(stock.QuantityReserved - reservedSum) > 0.001m)
            {
                logger.LogWarning(
                    "Reservation mismatch: product={ProductId} warehouse={WarehouseId} reserved={Reserved} entries={EntrySum}",
                    stock.ProductId, stock.WarehouseId, stock.QuantityReserved, reservedSum);
            }
        }
    }
}

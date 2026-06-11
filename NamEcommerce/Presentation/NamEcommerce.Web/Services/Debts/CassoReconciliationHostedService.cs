using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Web.Services.Debts;

public sealed class CassoReconciliationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<CassoReconciliationHostedService> logger) : BackgroundService
{
    private static readonly SemaphoreSlim RunLock = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = GetInterval();

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Casso reconciliation worker failed.");
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
            var settings = scope.ServiceProvider.GetRequiredService<BankTransferPaymentSettings>();

            if (!settings.Casso.Enabled
                || !settings.Casso.ReconciliationEnabled
                || !string.Equals(settings.Verification.Provider, "Casso", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var appService = scope.ServiceProvider.GetRequiredService<ICassoBankTransferAppService>();
            await appService.RunReconciliationAsync(new RunCassoReconciliationAppDto
            {
                Trigger = (int)CassoReconciliationRunTrigger.Scheduled
            }).ConfigureAwait(false);

            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            RunLock.Release();
        }
    }

    private TimeSpan GetInterval()
    {
        using var scope = scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<BankTransferPaymentSettings>();
        return TimeSpan.FromMinutes(Math.Max(1, settings.Casso.ReconciliationIntervalMinutes));
    }
}

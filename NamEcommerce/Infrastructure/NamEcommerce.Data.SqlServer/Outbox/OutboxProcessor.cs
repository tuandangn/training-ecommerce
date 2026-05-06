using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NamEcommerce.Domain.Entities.Outbox;
using NamEcommerce.Domain.Shared.Events;

namespace NamEcommerce.Data.SqlServer.Outbox;

/// <summary>
/// Background service đọc bảng <c>OutboxMessages</c> chưa processed,
/// publish event qua MediatR và đánh dấu đã xử lý.
/// <para>
/// Quy trình mỗi tick:
/// <list type="number">
///   <item>Tạo scope DI mới — service phụ thuộc vào <c>NamEcommerceEfDbContext</c> (scoped).</item>
///   <item>Query batch <see cref="OutboxProcessorOptions.BatchSize"/> message: <c>ProcessedOnUtc IS NULL</c> và <c>RetryCount &lt; MaxRetry</c>, sắp xếp theo <c>OccurredOnUtc</c> tăng dần.</item>
///   <item>Với từng message: deserialize Payload theo Type → publish qua <see cref="IPublisher"/>.</item>
///   <item>Thành công → <c>MarkAsProcessed()</c>; Lỗi → <c>MarkAsFailed(error)</c> + log.</item>
///   <item><c>SaveChangesAsync</c> ở cuối batch.</item>
/// </list>
/// </para>
/// <para>
/// Idempotency phụ thuộc vào handler — nếu cùng message được dispatch 2 lần, handler phải tự xử lý.
/// Concurrency: instance nhiều nên dùng SQL row lock (sẽ thêm khi scale-out, hiện tại single-instance).
/// </para>
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<OutboxProcessorOptions> _options;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<OutboxProcessorOptions> options,
        ILogger<OutboxProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var opts = _options.CurrentValue;
            try
            {
                await ProcessBatchAsync(opts, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor batch failed unexpectedly.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(opts.PollingIntervalSeconds), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("OutboxProcessor stopped.");
    }

    private async Task ProcessBatchAsync(OutboxProcessorOptions opts, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NamEcommerceEfDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < opts.MaxRetryCount)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(opts.BatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            await DispatchSingleAsync(message, publisher, cancellationToken).ConfigureAwait(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DispatchSingleAsync(
        OutboxMessage message,
        IPublisher publisher,
        CancellationToken cancellationToken)
    {
        try
        {
            var clrType = Type.GetType(message.Type, throwOnError: false);
            if (clrType is null)
            {
                message.MarkAsFailed($"Cannot resolve CLR type '{message.Type}'.");
                _logger.LogError("Outbox: unable to resolve type {TypeName} for message {Id}.", message.Type, message.Id);
                return;
            }

            if (JsonSerializer.Deserialize(message.Payload, clrType, SerializerOptions) is not IIntegrationEvent integrationEvent)
            {
                message.MarkAsFailed($"Deserialized payload is null or not an IIntegrationEvent (type='{message.Type}').");
                return;
            }

            await publisher.Publish(integrationEvent, cancellationToken).ConfigureAwait(false);
            message.MarkAsProcessed();
        }
        catch (Exception ex)
        {
            message.MarkAsFailed(ex.Message);
            _logger.LogWarning(ex, "Outbox: dispatch message {Id} (type={Type}) failed (retry={Retry}).",
                message.Id, message.Type, message.RetryCount);
        }
    }
}

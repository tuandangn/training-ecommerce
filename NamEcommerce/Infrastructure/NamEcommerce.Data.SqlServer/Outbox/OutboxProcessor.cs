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
///   <item>Query batch message IDs.</item>
///   <item>Mỗi message xử lý trong scope DI riêng — đảm bảo handler fail không ảnh hưởng message khác.</item>
///   <item>Thành công → <c>MarkAsProcessed()</c> + <c>SaveChanges</c>; Lỗi → <c>MarkAsFailed</c> + <c>SaveChanges</c> + log.</item>
/// </list>
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
        IReadOnlyList<Guid> messageIds;
        using (var readScope = _scopeFactory.CreateScope())
        {
            var db = readScope.ServiceProvider.GetRequiredService<NamEcommerceEfDbContext>();
            messageIds = await db.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc == null && m.RetryCount < opts.MaxRetryCount)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(opts.BatchSize)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var id in messageIds)
            await ProcessSingleAsync(id, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessSingleAsync(Guid messageId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NamEcommerceEfDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var message = await db.Set<OutboxMessage>()
            .FindAsync(new object?[] { messageId }, cancellationToken)
            .ConfigureAwait(false);

        if (message is null) return;

        try
        {
            var clrType = Type.GetType(message.Type, throwOnError: false);
            if (clrType is null)
            {
                message.MarkAsFailed($"Cannot resolve CLR type '{message.Type}'.");
                _logger.LogError("Outbox: unable to resolve type {TypeName} for message {Id}.", message.Type, message.Id);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            if (JsonSerializer.Deserialize(message.Payload, clrType, SerializerOptions) is not INotification notification)
            {
                message.MarkAsFailed($"Deserialized payload is null or not an INotification (type='{message.Type}').");
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            await publisher.Publish(notification, cancellationToken).ConfigureAwait(false);
            message.MarkAsProcessed();
        }
        catch (Exception ex)
        {
            message.MarkAsFailed(ex.Message);
            _logger.LogWarning(ex, "Outbox: dispatch message {Id} (type={Type}) failed (retry={Retry}).",
                message.Id, message.Type, message.RetryCount);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

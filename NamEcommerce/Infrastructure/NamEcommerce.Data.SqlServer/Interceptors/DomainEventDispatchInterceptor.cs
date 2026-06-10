using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NamEcommerce.Domain.Entities.Outbox;
using NamEcommerce.Domain.Shared.Events;

namespace NamEcommerce.Data.SqlServer.Interceptors;

/// <summary>
/// EF Core <see cref="SaveChangesInterceptor"/> thực hiện 2 việc:
/// <list type="number">
///   <item>
///     <term>SavingChanges</term>
///     <description>
///       Serialize mọi <see cref="IReliableDomainEvent"/> từ ChangeTracker thành <c>OutboxMessage</c>
///       rồi Add vào DbContext — đảm bảo atomic với business write trong cùng transaction.
///       Sau đó clear các reliable event khỏi aggregate.
///     </description>
///   </item>
///   <item>
///     <term>SavedChanges</term>
///     <description>
///       Dispatch các <see cref="IDomainEvent"/> còn lại (non-reliable) inline qua MediatR.
///     </description>
///   </item>
/// </list>
/// </summary>
public sealed class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatchInterceptor(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            SerializeReliableEventsToOutbox(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            SerializeReliableEventsToOutbox(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var resultValue = await base.SavedChangesAsync(eventData, result, cancellationToken)
            .ConfigureAwait(false);

        if (eventData.Context is not null)
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken).ConfigureAwait(false);

        return resultValue;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var resultValue = base.SavedChanges(eventData, result);

        if (eventData.Context is not null)
            DispatchDomainEventsAsync(eventData.Context, default).GetAwaiter().GetResult();

        return resultValue;
    }

    private static void SerializeReliableEventsToOutbox(DbContext context)
    {
        var aggregates = context.ChangeTracker
            .Entries<AppAggregateEntity>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Any(e => e is IReliableDomainEvent))
            .ToList();

        if (aggregates.Count == 0) return;

        foreach (var aggregate in aggregates)
        {
            var reliableEvents = aggregate.DomainEvents
                .OfType<IReliableDomainEvent>()
                .ToList();

            foreach (var evt in reliableEvents)
            {
                var clrType = evt.GetType();
                var typeName = clrType.AssemblyQualifiedName
                    ?? throw new InvalidOperationException(
                        $"Cannot resolve AssemblyQualifiedName for type '{clrType.FullName}'.");

                var payload = JsonSerializer.Serialize(evt, clrType, SerializerOptions);
                var message = OutboxMessage.Create(typeName, payload, ((IDomainEvent)evt).OccurredOnUtc);
                context.Add(message);
            }

            aggregate.ClearDomainEvents(e => e is IReliableDomainEvent);
        }
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        var aggregates = context.ChangeTracker
            .Entries<AppAggregateEntity>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        if (aggregates.Count == 0) return;

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        var publisher = _serviceProvider.GetRequiredService<IPublisher>();

        foreach (var domainEvent in events)
            await publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
    }
}

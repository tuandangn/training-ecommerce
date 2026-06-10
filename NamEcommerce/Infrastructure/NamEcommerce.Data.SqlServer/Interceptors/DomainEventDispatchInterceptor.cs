using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NamEcommerce.Domain.Entities.Outbox;
using NamEcommerce.Domain.Shared.Events;

namespace NamEcommerce.Data.SqlServer.Interceptors;

/// <summary>
/// EF Core <see cref="SaveChangesInterceptor"/>: tại <c>SavingChanges</c>, serialize MỌI
/// <see cref="IDomainEvent"/> từ ChangeTracker thành <c>OutboxMessage</c> rồi Add vào DbContext —
/// atomic với business write trong cùng transaction. <c>OutboxProcessor</c> sẽ publish qua MediatR
/// trong scope DI riêng (commit riêng + retry + dead-letter).
/// <para>
/// KHÔNG còn inline dispatch post-commit: handler chạy inline trên cùng DbContext vừa gây
/// duplicate tracking (load lại entity đã tracked qua reader rồi attach), vừa mất write
/// (stage sau lần SaveChanges duy nhất của request).
/// </para>
/// </summary>
public sealed class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            SerializeEventsToOutbox(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            SerializeEventsToOutbox(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SerializeEventsToOutbox(DbContext context)
    {
        var aggregates = context.ChangeTracker
            .Entries<AppAggregateEntity>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        if (aggregates.Count == 0) return;

        foreach (var aggregate in aggregates)
        {
            foreach (var evt in aggregate.DomainEvents)
            {
                var clrType = evt.GetType();
                var typeName = clrType.AssemblyQualifiedName
                    ?? throw new InvalidOperationException(
                        $"Cannot resolve AssemblyQualifiedName for type '{clrType.FullName}'.");

                var payload = JsonSerializer.Serialize(evt, clrType, SerializerOptions);
                var message = OutboxMessage.Create(typeName, payload, evt.OccurredOnUtc);
                context.Add(message);
            }

            aggregate.ClearDomainEvents();
        }
    }
}

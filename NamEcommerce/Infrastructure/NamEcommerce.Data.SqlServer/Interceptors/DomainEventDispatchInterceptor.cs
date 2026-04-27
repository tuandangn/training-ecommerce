using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NamEcommerce.Data.SqlServer.Interceptors;

/// <summary>
/// EF Core <see cref="SaveChangesInterceptor"/> dispatch toàn bộ Domain Event được raise bởi các Aggregate
/// đang được EF tracked, sau khi <c>SaveChanges</c> hoàn tất thành công.
/// <para>Quy trình:</para>
/// <list type="number">
///   <item>Sau <c>SavedChangesAsync</c> — quét <see cref="DbContext.ChangeTracker"/> tìm các <see cref="AppAggregateEntity"/>.</item>
///   <item>Thu thập <see cref="AppAggregateEntity.DomainEvents"/> từ mỗi entity.</item>
///   <item>Clear domain events trên entity (tránh re-publish nếu cùng entity được SaveChanges lần nữa).</item>
///   <item>Publish từng event qua <see cref="IPublisher"/> (MediatR).</item>
/// </list>
/// </summary>
public sealed class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    public DomainEventDispatchInterceptor(IPublisher publisher)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        _publisher = publisher;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
        }

        await DispatchDomainEventsAsync(context, cancellationToken).ConfigureAwait(false);

        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return base.SavedChanges(eventData, result);
        }

        // Async dispatch trên sync path — block để đảm bảo hoàn tất trước khi return.
        DispatchDomainEventsAsync(context, CancellationToken.None).GetAwaiter().GetResult();

        return base.SavedChanges(eventData, result);
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
        {
            aggregate.ClearDomainEvents();
        }

        foreach (var domainEvent in events)
        {
            await _publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}

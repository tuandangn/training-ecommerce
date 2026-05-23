using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

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
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatchInterceptor(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var resultValue = await base.SavedChangesAsync(eventData, result, cancellationToken)
            .ConfigureAwait(false);

        var context = eventData.Context;
        if (context is not null)
            await DispatchDomainEventsAsync(context, cancellationToken).ConfigureAwait(false);

        return resultValue;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var resultValue = base.SavedChanges(eventData, result);

        var context = eventData.Context;
        if (context is not null)
            DispatchDomainEventsAsync(context, default).GetAwaiter().GetResult();

        return resultValue;
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
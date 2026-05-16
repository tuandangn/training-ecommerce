using System.Text.Json;
using NamEcommerce.Domain.Entities.Outbox;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Services.Outbox;

namespace NamEcommerce.Data.SqlServer.Outbox;

/// <summary>
/// Implementation của <see cref="IOutbox"/> dùng EF Core.
/// <para>
/// <c>AddAsync</c> chỉ <c>Add</c> entity vào <see cref="NamEcommerceEfDbContext"/> — KHÔNG gọi <c>SaveChanges</c>.
/// Việc persist phụ thuộc vào <c>SaveChanges</c> của business operation đang chạy → đảm bảo cùng transaction.
/// </para>
/// </summary>
public sealed class OutboxAccessor : IOutbox
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly NamEcommerceEfDbContext _dbContext;

    public OutboxAccessor(NamEcommerceEfDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task AddAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var concreteType = integrationEvent.GetType();
        var typeName = concreteType.AssemblyQualifiedName
            ?? throw new InvalidOperationException(
                $"Cannot resolve AssemblyQualifiedName for type '{concreteType.FullName}'.");

        var payload = JsonSerializer.Serialize(integrationEvent, concreteType, SerializerOptions);

        var message = OutboxMessage.Create(typeName, payload, integrationEvent.OccurredOnUtc);
        await _dbContext.AddAsync(message, cancellationToken).ConfigureAwait(false);
    }
}

using NamEcommerce.Domain.Shared.Events;

namespace NamEcommerce.Domain.Shared.Services.Outbox;

/// <summary>
/// Cổng (port) để code nghiệp vụ enqueue Integration Event.
/// <para>
/// Implementation nằm ở Infrastructure (Data.SqlServer) — sẽ insert một <c>OutboxMessage</c>
/// vào DbContext. Vì cùng DbContext (cùng transaction) với business operation, message và state
/// đảm bảo atomic (Transactional Outbox Pattern).
/// </para>
/// <para>
/// Background service <c>OutboxProcessor</c> đọc các message chưa processed,
/// publish qua MediatR và đánh dấu đã xử lý.
/// </para>
/// </summary>
public interface IOutbox
{
    /// <summary>
    /// Enqueue một integration event vào Outbox. Message sẽ được persist khi
    /// <c>SaveChanges</c>/<c>SaveChangesAsync</c> được gọi.
    /// </summary>
    /// <param name="integrationEvent">Event cần publish ra ngoài hệ thống.</param>
    /// <param name="cancellationToken">Token huỷ tác vụ.</param>
    Task AddAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}

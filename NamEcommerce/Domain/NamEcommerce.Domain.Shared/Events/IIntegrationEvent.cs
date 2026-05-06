using MediatR;

namespace NamEcommerce.Domain.Shared.Events;

/// <summary>
/// Marker interface cho Integration Event — event vượt qua biên giới (boundary) của bounded context hiện tại,
/// thường được publish ra hệ thống bên ngoài (n8n, message broker, third-party API…).
/// <para>
/// Khác với <see cref="IDomainEvent"/> (xử lý trong cùng process sau <c>SaveChanges</c>),
/// Integration Event được lưu vào bảng <c>OutboxMessages</c> trong cùng transaction nghiệp vụ
/// rồi được dispatch tách rời (eventually consistent) bởi <c>OutboxProcessor</c> background service.
/// </para>
/// <para>
/// Pattern này đảm bảo: nếu transaction nghiệp vụ rollback thì integration event cũng KHÔNG được publish;
/// và nếu hệ thống ngoài tạm thời lỗi, event vẫn được retry mà không mất.
/// </para>
/// <para>
/// Kế thừa <see cref="INotification"/> để <c>OutboxProcessor</c> có thể publish qua MediatR sau khi deserialize.
/// </para>
/// </summary>
public interface IIntegrationEvent : INotification
{
    /// <summary>
    /// Định danh duy nhất của integration event — dùng cho idempotency check ở phía consumer
    /// và trace, debug.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Thời điểm event xảy ra (UTC) — KHÔNG phải thời điểm được dispatch.
    /// </summary>
    DateTime OccurredOnUtc { get; }
}

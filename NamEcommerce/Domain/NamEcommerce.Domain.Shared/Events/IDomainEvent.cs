using MediatR;

namespace NamEcommerce.Domain.Shared.Events;

/// <summary>
/// Marker interface cho Domain Event — event mang ngữ nghĩa nghiệp vụ được raise bởi Aggregate.
/// Kế thừa <see cref="INotification"/> để dispatch qua MediatR sau khi <c>SaveChanges</c> hoàn tất.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Định danh duy nhất của lần raise event — dùng để trace, debug, idempotency check.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Thời điểm event được raise (UTC).
    /// </summary>
    DateTime OccurredOnUtc { get; }
}

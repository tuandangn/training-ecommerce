namespace NamEcommerce.Domain.Shared.Events;

/// <summary>
/// Domain Event đánh dấu cần đảm bảo độ tin cậy qua Outbox Pattern.
/// Event sẽ được serialize vào <c>OutboxMessages</c> trong cùng transaction với nghiệp vụ,
/// thay vì dispatch inline sau <c>SaveChanges</c>.
/// <para>Kế thừa <see cref="IIntegrationEvent"/> để <c>OutboxProcessor</c> có thể deserialize và publish.</para>
/// </summary>
public interface IReliableDomainEvent : IDomainEvent, IIntegrationEvent
{
}

using System.ComponentModel.DataAnnotations.Schema;
using NamEcommerce.Domain.Shared.Events;

namespace NamEcommerce.Domain.Shared;

public record AppAggregateEntity : AppEntity, ISoftDeletable
{
    [NotMapped]
    private readonly List<IDomainEvent> _domainEvents = new();

    public AppAggregateEntity(Guid id) : base(id)
    {
    }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedOnUtc { get; private set; }

    #region Events

    /// <summary>
    /// Danh sách Domain Event đã được raise nhưng chưa dispatch.
    /// EF không persist field này (đã đánh dấu <see cref="NotMappedAttribute"/>).
    /// </summary>
    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void Delete()
    {
        IsDeleted = true;
        DeletedOnUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Aggregate gọi method này để raise Domain Event — event sẽ dispatch sau khi <c>SaveChanges</c> thành công.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Xoá toàn bộ Domain Event đã raise. Gọi sau khi dispatch xong để tránh re-publish trong các lần SaveChanges sau.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    #endregion
}

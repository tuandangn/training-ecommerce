using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Events.Customers;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Customers;

[Serializable]
public sealed record Customer : AppAggregateEntity
{
    internal Customer(Guid id, string fullName, string phoneNumber, string address)
        : base(id)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
        Address = address;

        CreatedOnUtc = DateTime.UtcNow;
    }

    public string FullName
    {
        get;
        internal set
        {
            field = value;
            NormalizedFullName = TextHelper.Normalize(value);
        }
    }
    internal string NormalizedFullName { get; private set; } = "";

    public string Address
    {
        get;
        internal set
        {
            field = value;
            NormalizedAddress = TextHelper.Normalize(value);
        }
    }
    internal string NormalizedAddress { get; private set; } = "";

    public string PhoneNumber { get; internal set; }

    public string? Email { get; internal set; }
    public string? Note { get; internal set; }
    public DateTime CreatedOnUtc { get; init; }

    /// <summary>
    /// Đánh dấu khách hàng vừa được khởi tạo — Manager gọi trước <c>InsertAsync</c>.
    /// </summary>
    internal void MarkCreated()
        => RaiseDomainEvent(new CustomerCreated(Id, FullName, PhoneNumber));

    /// <summary>
    /// Đánh dấu khách hàng vừa được cập nhật — raise <see cref="CustomerUpdated"/>.
    /// </summary>
    internal void MarkUpdated()
        => RaiseDomainEvent(new CustomerUpdated(Id));

    /// <summary>
    /// Đánh dấu khách hàng bị xoá — Manager gọi trước <c>DeleteAsync</c> để raise <see cref="CustomerDeleted"/>.
    /// </summary>
    internal void MarkDeleted()
        => RaiseDomainEvent(new CustomerDeleted(Id, FullName));
}

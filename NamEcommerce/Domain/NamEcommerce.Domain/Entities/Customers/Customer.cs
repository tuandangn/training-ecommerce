using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Customers;

[Serializable]
public sealed record Customer : AppAggregateEntity
{
    public Customer(Guid id, string fullName, string phoneNumber, string address)
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
}

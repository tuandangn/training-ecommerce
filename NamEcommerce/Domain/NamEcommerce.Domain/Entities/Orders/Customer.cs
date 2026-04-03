using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Orders;

[Serializable]
public sealed record Customer : AppAggregateEntity
{
    private Customer() : base(Guid.Empty) { } // for EF

    public Customer(Guid id, string fullName, string phoneNumber, string? email = null, string? address = null, string? note = null)
        : base(id)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
        Email = email;
        Address = address;
        Note = note;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string FullName { get; internal set; }
    public string PhoneNumber { get; internal set; }
    public string? Email { get; internal set; }
    public string? Address { get; internal set; }
    public string? Note { get; internal set; }
    public DateTime CreatedOnUtc { get; init; }
}

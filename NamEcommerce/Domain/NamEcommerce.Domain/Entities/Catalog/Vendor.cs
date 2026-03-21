using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record Vendor : AppAggregateEntity
{
    public Vendor(Guid id, string name, string phoneNumber) : base(id)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(phoneNumber);

        (Name, PhoneNumber) = (name, phoneNumber);
        NormalizedName = TextHelper.Normalize(Name);
        NormalizedAddress = TextHelper.Normalize(Address);
    }

    public string Name { get; init; }
    internal string NormalizedName { get; set; } = "";
    public string PhoneNumber { get; init; }
    public string? Address { get; set; }
    internal string NormalizedAddress { get; set; } = "";

    public int DisplayOrder { get; set; }
}

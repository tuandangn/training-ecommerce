using NamEcommerce.Domain.Metadata;

namespace NamEcommerce.Domain.Values;

[Serializable]
public sealed record CustomerInfo(NormalizableString FullName, string PhoneNumber, NormalizableString Address)
{
    public bool IsRetailWalkInCustomer { get; set; }
}

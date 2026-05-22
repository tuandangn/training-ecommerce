using NamEcommerce.Domain.Metadata;

namespace NamEcommerce.Domain.Values;

[Serializable]
public sealed record VendorInfo(NormalizableString Name, string? Phone, NormalizableString Address);
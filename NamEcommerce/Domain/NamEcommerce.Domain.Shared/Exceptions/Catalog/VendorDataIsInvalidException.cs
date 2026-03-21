namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class VendorDataIsInvalidException : Exception
{
    public VendorDataIsInvalidException(string? message) : base(message)
    {
    }
}

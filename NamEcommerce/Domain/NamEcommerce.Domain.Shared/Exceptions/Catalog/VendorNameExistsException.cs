namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class VendorNameExistsException : Exception
{
    public VendorNameExistsException(string name) : base($"Vendor with name '{name}' exists")
    {
    }
}

namespace NamEcommerce.Domain.Shared.Exceptions.Catalog;

[Serializable]
public sealed class VendorNameExistsException(string name) : Exception($"Vendor with name '{name}' exists");

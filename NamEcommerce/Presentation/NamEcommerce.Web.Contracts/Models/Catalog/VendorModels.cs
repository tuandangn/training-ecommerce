namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed record VendorModel
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Address { get; set; }
    public int DisplayOrder { get; set; }
}

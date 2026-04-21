namespace NamEcommerce.Web.Contracts.Configurations;

[Serializable]
public sealed class InfoOptions
{
    public required string Name { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Address { get; set; }
}

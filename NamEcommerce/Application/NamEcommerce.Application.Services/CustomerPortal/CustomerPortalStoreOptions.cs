namespace NamEcommerce.Application.Services.CustomerPortal;

public sealed class CustomerPortalStoreOptions
{
    public const string SectionName = "CustomerPortal:StoreContact";

    public string StoreName { get; set; } = "VLXD Tuấn Khôi";
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? MapQuery { get; set; }
}

namespace NamEcommerce.Web.Contracts.Configurations;

[Serializable]
public sealed class CultureConfig
{
    public string DefaultCulture { get; set; } = "vi-VN";
    public string[] SupportedCultures { get; set; } = ["vi-VN"];
}

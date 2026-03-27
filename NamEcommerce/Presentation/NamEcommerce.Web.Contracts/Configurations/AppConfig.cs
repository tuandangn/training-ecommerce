namespace NamEcommerce.Web.Contracts.Configurations;

[Serializable]
public sealed class AppConfig
{
    public bool AllowRegisterUser { get; set; }

    public int DefaultPageSize { get; set; }
    public int[] PageSizeOptions { get; set; } = [];

    public int UploadFileMaxSizeInBytes { get; set; }

    public string BreadcrumbSeparator { get; set; } = ">";
}

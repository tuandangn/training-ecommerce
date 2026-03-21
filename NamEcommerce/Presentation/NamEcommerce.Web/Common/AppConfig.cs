namespace NamEcommerce.Web.Common;

[Serializable]
public sealed class AppConfig
{
    public bool AllowRegisterUser { get; set; }

    public int DefaultPageSize { get; set; }
    public int[] PageSizeOptions { get; set; }
}

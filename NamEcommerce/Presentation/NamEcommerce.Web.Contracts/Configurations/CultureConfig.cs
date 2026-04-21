namespace NamEcommerce.Web.Contracts.Configurations;

[Serializable]
public sealed class CultureConfig
{
    /// <summary>
    /// Culture mặc định của hệ thống.
    /// Ví dụ: "vi-VN", "en-US"
    /// </summary>
    public string DefaultCulture { get; set; } = "vi-VN";

    /// <summary>
    /// Danh sách các culture được hỗ trợ.
    /// Phải chứa ít nhất DefaultCulture.
    /// </summary>
    public string[] SupportedCultures { get; set; } = ["vi-VN"];
}

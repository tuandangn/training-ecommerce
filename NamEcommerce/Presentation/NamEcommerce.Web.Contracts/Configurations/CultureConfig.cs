namespace NamEcommerce.Web.Contracts.Configurations;

[Serializable]
public sealed class CultureConfig
{
    /// <summary>Culture mặc định. Ví dụ: "vi-VN", "en-US"</summary>
    public string DefaultCulture { get; set; } = "vi-VN";

    /// <summary>Danh sách culture được hỗ trợ. Phải chứa ít nhất DefaultCulture.</summary>
    public string[] SupportedCultures { get; set; } = ["vi-VN"];

    // ── Flatpickr ────────────────────────────────────────────────────────────

    /// <summary>
    /// Locale code cho Flatpickr.
    /// Xem danh sách tại: https://flatpickr.js.org/localization/
    /// Ví dụ: "vn" (Việt Nam), "en" (Anh), "ja" (Nhật)
    /// </summary>
    public string FlatpickrLocale { get; set; } = "vn";

    /// <summary>
    /// Định dạng ngày hiển thị cho người dùng (Flatpickr altFormat).
    /// Ví dụ: "d/m/Y" → 25/12/2025
    /// Xem format tokens: https://flatpickr.js.org/formatting/
    /// </summary>
    public string FlatpickrDateFormat { get; set; } = "d/m/Y";

    /// <summary>
    /// Định dạng ngày giờ hiển thị cho người dùng (Flatpickr altFormat).
    /// Ví dụ: "d/m/Y H:i" → 25/12/2025 14:30
    /// </summary>
    public string FlatpickrDateTimeFormat { get; set; } = "d/m/Y H:i";
}

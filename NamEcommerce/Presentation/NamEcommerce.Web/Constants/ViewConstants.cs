namespace NamEcommerce.Web.Constants;

public static class ViewConstants
{
    public const string PageTitle = "Title";

    public const string TitleSection = "TitleSection";
    public const string MessageSection = "MessageSection";
    public const string StyleSection = "Styles";
    public const string ScriptSection = "Scripts";

    // Toàn bộ các hằng XxxSuccessMessage / XxxErrorMessage / GlobalErrorMessage / LoginErrorMessage
    // đã được loại bỏ vào 2026-04-25 — đã thay bằng INotificationService
    // (xem NamEcommerce.Web.Services.Notifications.INotificationService).

    public const string NumberCustomFormat = "#,##0.##";
    public const string CurrencyDisplayFormat = $"#,##0.## {DefaultCurrencySymbol}";
    public const string DefaultDateFormat = "dd/MM/yyyy";
    public const string DefaultDateTimeFormat = "dd/MM/yyyy hh:mm";
    public const string DefaultCurrencySymbol = "đ";
}

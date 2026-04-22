namespace NamEcommerce.Web.Extensions;

public static class DateTimeExtensions
{
    private const string DateFormat = "dd/MM/yyyy";
    private const string DateTimeFormat = "dd/MM/yyyy HH:mm";

    public static string DisplayDate(this DateTime dt) => dt.ToString(DateFormat);
    public static string DisplayDateTime(this DateTime dt) => dt.ToString(DateTimeFormat);

    public static string DisplayDate(this DateTime? dt)
        => dt.HasValue ? dt.Value.DisplayDate() : string.Empty;

    public static string DisplayDateTime(this DateTime? dt)
        => dt.HasValue ? dt.Value.DisplayDateTime() : string.Empty;
}

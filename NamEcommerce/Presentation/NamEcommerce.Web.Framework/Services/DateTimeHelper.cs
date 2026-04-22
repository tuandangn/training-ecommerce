namespace NamEcommerce.Web.Framework.Services;

public sealed class DateTimeHelper
{
    // "SE Asia Standard Time" trên Windows; "Asia/Ho_Chi_Minh" trên Linux/macOS
    private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }

    /// <summary>
    /// Lấy thời gian hiện tại theo giờ Việt Nam (UTC+7).
    /// </summary>
    public static DateTime NowVietnam()
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);

    /// <summary>
    /// Chuyển một DateTime (được nhập theo giờ Việt Nam) sang UTC để lưu vào database.
    /// </summary>
    public static DateTime ToUniversalTime(DateTime vietnamTime)
        => ToUniversalTime((DateTime?)vietnamTime)!.Value;

    /// <summary>
    /// Chuyển một DateTime? (được nhập theo giờ Việt Nam) sang UTC để lưu vào database.
    /// </summary>
    public static DateTime? ToUniversalTime(DateTime? vietnamTime)
    {
        if (!vietnamTime.HasValue)
            return null;

        // Nếu Kind đã là Utc thì không chuyển lại
        if (vietnamTime.Value.Kind == DateTimeKind.Utc)
            return vietnamTime.Value;

        var unspecified = DateTime.SpecifyKind(vietnamTime.Value, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, VietnamTimeZone);
    }

    /// <summary>
    /// Chuyển UTC từ database sang giờ Việt Nam để hiển thị.
    /// </summary>
    public static DateTime ToLocalTime(DateTime utcTime)
        => ToLocalTime((DateTime?)utcTime)!.Value;

    /// <summary>
    /// Chuyển UTC? từ database sang giờ Việt Nam để hiển thị.
    /// </summary>
    public static DateTime? ToLocalTime(DateTime? utcTime)
    {
        if (!utcTime.HasValue)
            return null;

        var asUtc = DateTime.SpecifyKind(utcTime.Value, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(asUtc, VietnamTimeZone);
    }
}

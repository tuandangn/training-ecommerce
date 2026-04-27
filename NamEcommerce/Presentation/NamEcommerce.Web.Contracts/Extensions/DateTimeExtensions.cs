namespace NamEcommerce.Web.Contracts.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ToEndOfDate(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 23, 59, 59, 999, dateTime.Kind);
    }

    public static DateTime? ToEndOfDate(this DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;

        return ToEndOfDate(dateTime.Value);
    }
}

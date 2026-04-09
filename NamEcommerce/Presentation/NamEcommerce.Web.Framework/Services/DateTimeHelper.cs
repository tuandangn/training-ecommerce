using MediatR;

namespace NamEcommerce.Web.Framework.Services;

public sealed class DateTimeHelper
{
    public static DateTime ToUniversalTime(DateTime localTime)
        => ToUniversalTime((DateTime?)localTime)!.Value;

    public static DateTime? ToUniversalTime(DateTime? localTime)
    {
        DateTime? universalTime = localTime;
        if (universalTime.HasValue)
        {
            universalTime = universalTime.Value.Date
                .AddHours(DateTime.Now.Hour)
                .AddMinutes(DateTime.Now.Minute)
                .AddSeconds(DateTime.Now.Second)
                .AddMilliseconds(DateTime.Now.Millisecond)
                .AddMicroseconds(DateTime.Now.Microsecond)
                .ToUniversalTime();
        }
        return universalTime;
    }
}

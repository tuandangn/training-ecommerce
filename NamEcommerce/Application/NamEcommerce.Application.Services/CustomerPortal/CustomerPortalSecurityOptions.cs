namespace NamEcommerce.Application.Services.CustomerPortal;

public sealed class CustomerPortalSecurityOptions
{
    public const string SectionName = "CustomerPortal:Security";

    public int OtpExpiryMinutes { get; set; } = 5;
    public int SessionExpiryHours { get; set; } = 8;
    public int OtpCooldownSeconds { get; set; } = 60;
    public int MaxOtpRequestsPerCustomerPerHour { get; set; } = 5;
    public int MaxOtpRequestsPerIpPerHour { get; set; } = 20;
    public int MaxPasswordFailuresPerCustomerPerHour { get; set; } = 5;
    public int MaxPasswordFailuresPerIpPerHour { get; set; } = 20;
    public int DeliveryAccessTokenExpiryDays { get; set; } = 30;
    public bool RevokeExistingDeliveryTokensOnCreate { get; set; }

    public int SafeOtpExpiryMinutes => Math.Max(1, OtpExpiryMinutes);
    public int SafeSessionExpiryHours => Math.Max(1, SessionExpiryHours);
    public int SafeOtpCooldownSeconds => Math.Max(1, OtpCooldownSeconds);
    public int SafeMaxOtpRequestsPerCustomerPerHour => Math.Max(1, MaxOtpRequestsPerCustomerPerHour);
    public int SafeMaxOtpRequestsPerIpPerHour => Math.Max(1, MaxOtpRequestsPerIpPerHour);
    public int SafeMaxPasswordFailuresPerCustomerPerHour => Math.Max(1, MaxPasswordFailuresPerCustomerPerHour);
    public int SafeMaxPasswordFailuresPerIpPerHour => Math.Max(1, MaxPasswordFailuresPerIpPerHour);
    public int SafeDeliveryAccessTokenExpiryDays => Math.Max(1, DeliveryAccessTokenExpiryDays);
}

namespace NamEcommerce.Customer.Api.Infrastructure;

public sealed class CustomerPortalAuthCookieOptions
{
    public const string SectionName = "CustomerPortal:AuthCookie";

    public bool CrossSite { get; init; }
}

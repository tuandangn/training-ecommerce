using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Customer.Contracts.Models;

namespace NamEcommerce.Customer.Api.Infrastructure;

internal sealed class CustomerSessionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICustomerPortalAuthAppService authAppService)
    {
        if (context.Request.Cookies.TryGetValue(CustomerPortalAuthDefaults.SessionCookieName, out var token))
        {
            var session = await authAppService.GetSessionAsync(token, DateTime.UtcNow).ConfigureAwait(false);
            if (session is not null)
            {
                context.Items[CustomerPortalAuthDefaults.SessionItemKey] = new CustomerSessionModel(
                    session.SessionId,
                    session.CustomerId,
                    session.CustomerName,
                    session.PhoneNumber,
                    session.Email,
                    session.HasPassword,
                    session.ExpiresOnUtc);
            }
        }

        if (RequiresCustomerSession(context.Request.Path) &&
            context.Items[CustomerPortalAuthDefaults.SessionItemKey] is not CustomerSessionModel)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool RequiresCustomerSession(PathString path)
        => path.StartsWithSegments("/api/me", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/api/orders", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/api/order-requests", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/api/delivery-notes", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/api/return-requests", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/api/debts", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/api/payment-intents", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/api/products", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/api/auth/password/set", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/api/auth/password/change", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/api/auth/logout", StringComparison.OrdinalIgnoreCase);
}

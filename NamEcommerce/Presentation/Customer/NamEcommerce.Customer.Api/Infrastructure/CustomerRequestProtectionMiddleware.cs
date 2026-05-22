namespace NamEcommerce.Customer.Api.Infrastructure;

internal sealed class CustomerRequestProtectionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresProtection(context.Request) &&
            context.Request.Headers[CustomerPortalAuthDefaults.RequestHeaderName] != "1")
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool RequiresProtection(HttpRequest request)
        => request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
           && (HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method) || HttpMethods.IsPatch(request.Method) || HttpMethods.IsDelete(request.Method));
}

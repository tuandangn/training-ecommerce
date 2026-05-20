namespace NamEcommerce.Customer.Api.Infrastructure;

internal static class CustomerApiApplicationBuilderExtensions
{
    internal static WebApplication UseCustomerPortalApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseHttpsRedirection();
        app.UseCors("CustomerClient");
        app.UseRateLimiter();
        app.UseMiddleware<CustomerRequestProtectionMiddleware>();
        app.UseMiddleware<CustomerSessionMiddleware>();
        app.MapControllers();

        return app;
    }
}

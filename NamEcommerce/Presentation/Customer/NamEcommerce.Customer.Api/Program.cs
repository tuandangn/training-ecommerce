using NamEcommerce.Customer.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomerPortalApi(builder.Configuration);

var app = builder.Build();

app.UseCustomerPortalApi();

app.Run();

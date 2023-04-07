using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using NamEcommerce.Admin.Client;
using NamEcommerce.Admin.Client.GraphQl;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.Configure<GraphQlOptions>(options =>
{
    builder.Configuration.Bind("NamEcommerce:GraphQl", options);
});
builder.Services.AddScoped(services 
    => services.GetRequiredService<IOptionsSnapshot<GraphQlOptions>>().Value);
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssemblyContaining<App>();
});
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

await builder.Build().RunAsync();

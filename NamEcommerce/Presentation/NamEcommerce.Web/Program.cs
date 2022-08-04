using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Data.MongoDb;
using NamEcommerce.Domain.Services.Catalog;
using NamEcommerce.Domain.Shared.Services;

//services
var builder = WebApplication.CreateBuilder(args);
ConfigureServices(builder.Services, builder.Configuration);

//middlewares
var app = builder.Build();
Configure(app);

//start
app.Run();

#region Local Methods

void ConfigureServices(IServiceCollection services, ConfigurationManager configuration)
{
    services.AddControllersWithViews();

    services.AddAuthentication(opts =>
        opts.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme
    ).AddCookie(opts =>
    {
        opts.LoginPath = "/User/Login";
        opts.LogoutPath = "/User/Logout";
    });

    services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

    services.AddSingleton<IDbContext>(sp
        => new NamEcommerceMongoDbContext(configuration.GetConnectionString(nameof(NamEcommerceMongoDbContext))));
}

void Configure(WebApplication app)
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
}

#endregion

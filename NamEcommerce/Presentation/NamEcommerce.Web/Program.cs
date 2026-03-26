using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Application.Services.Catalog;
using NamEcommerce.Application.Services.Media;
using NamEcommerce.Application.Services.Queries.Handlers.Catalog;
using NamEcommerce.Application.Services.Users;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Data.SqlServer;
using NamEcommerce.Domain.Services.Catalog;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Services.Security;
using NamEcommerce.Domain.Services.Users;
using NamEcommerce.Domain.Shared.Services;
using NamEcommerce.Domain.Shared.Services.Catalog;
using NamEcommerce.Domain.Shared.Services.Media;
using NamEcommerce.Web.Common;
using NamEcommerce.Web.Contracts.Services;
using NamEcommerce.Web.Framework.Commands.Handlers;
using NamEcommerce.Web.Framework.Services;
using NamEcommerce.Web.Mvc.Binders;
using NamEcommerce.Web.Services;
using NamEcommerce.Web.Validators;
using System.Reflection;

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
    //options
    services.Configure<AppConfig>(options =>
    {
        builder.Configuration.Bind(AppConstants.AppConfigSectionName, options);
    });
    services.AddScoped(services
        => services.GetRequiredService<IOptionsSnapshot<AppConfig>>().Value);

    services.Configure<InfoOptions>(options =>
    {
        builder.Configuration.Bind(AppConstants.InfoSectionName, options);
    });
    services.AddScoped(services
        => services.GetRequiredService<IOptionsSnapshot<InfoOptions>>().Value);

    //infrastructure services
    services.AddDbContext<NamEcommerceEfDbContext>(opts =>
        opts.UseSqlServer(configuration.GetConnectionString(nameof(NamEcommerceEfDbContext)),
            opt => opt.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name))
    );
    services.AddScoped<IDbContext, NamEcommerceEfDbContext>();
    services.AddScoped(typeof(IRepository<>), typeof(NamEcommerceEfRepository<>));
    services.AddScoped(typeof(IEntityDataReader<>), typeof(EntityDataReader<>));

    services.AddAuthentication(opts =>
        opts.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme
    ).AddCookie(opts =>
    {
        opts.LoginPath = "/User/Login";
        opts.LogoutPath = "/User/Logout";
    });
    services.AddHttpContextAccessor();

    //domain service
    services.AddScoped<IUserManager, UserManager>();
    services.AddScoped<ICategoryManager, CategoryManager>();
    services.AddScoped<IUnitMeasurementManager, UnitMeasurementManager>();
    services.AddScoped<IVendorManager, VendorManager>();
    services.AddScoped<IProductManager, ProductManager>();
    services.AddScoped<IPictureManager, PictureManager>();

    services.AddScoped<ISecurityService, SecurityService>();

    services.AddScoped<ICategoryAppService, CategoryAppService>();
    services.AddScoped<IUnitMeasurementAppService, UnitMeasurementAppService>();
    services.AddScoped<IUserAppService, UserAppService>();
    services.AddScoped<IVendorAppService, VendorAppService>();
    services.AddScoped<IProductAppService, ProductAppService>();
    services.AddScoped<IPictureAppService, PictureAppService>();

    //application services
    services.AddScoped<IInformationService, InformationService>();
    services.AddScoped<ICurrentUserService, CurrentUserService>();
    services.AddScoped<IWebHelper, WebHelper>();

    services.AddMediatR(config =>
    {
        //NamEcommerce.Application.Services assembly
        config.RegisterServicesFromAssemblyContaining<GetAllCategoriesHandler>();
        //NamEcommerce.Web.Framework assembly
        config.RegisterServicesFromAssemblyContaining<CookieAuthenticateUserHandler>();
    });

    services.AddSession();

    //mvc
    services.AddMvc(options =>
    {
        options.ModelBinderProviders.Insert(0, new TrimModelBinderProvider());
    }).AddSessionStateTempDataProvider();

    services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
    services.AddValidatorsFromAssemblyContaining<LoginModelValidator>();
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
    app.UseSession();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
}

#endregion

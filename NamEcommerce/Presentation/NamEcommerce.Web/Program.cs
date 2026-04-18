using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Communication;
using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Preparation;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Contracts.Report;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Application.Services.Catalog;
using NamEcommerce.Application.Services.Communication;
using NamEcommerce.Application.Services.Customers;
using NamEcommerce.Application.Services.Debts;
using NamEcommerce.Application.Services.DeliveryNotes;
using NamEcommerce.Application.Services.Events;
using NamEcommerce.Application.Services.Finance;
using NamEcommerce.Application.Services.Inventory;
using NamEcommerce.Application.Services.Media;
using NamEcommerce.Application.Services.Orders;
using NamEcommerce.Application.Services.Preparation;
using NamEcommerce.Application.Services.PurchaseOrders;
using NamEcommerce.Application.Services.Report;
using NamEcommerce.Application.Services.Users;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Data.SqlServer;
using NamEcommerce.Domain.Services.Catalog;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Services.Customers;
using NamEcommerce.Domain.Services.Debts;
using NamEcommerce.Domain.Services.DeliveryNotes;
using NamEcommerce.Domain.Services.Finance;
using NamEcommerce.Domain.Services.Inventory;
using NamEcommerce.Domain.Services.Media;
using NamEcommerce.Domain.Services.Orders;
using NamEcommerce.Domain.Services.PurchaseOrders;
using NamEcommerce.Domain.Services.Security;
using NamEcommerce.Domain.Services.Users;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Services.Catalog;
using NamEcommerce.Domain.Shared.Services.Customers;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Finance;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Media;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.Security;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Services;
using NamEcommerce.Web.Framework.Commands.Handlers.Users;
using NamEcommerce.Web.Framework.Services;
using NamEcommerce.Web.Mvc.Binders;
using NamEcommerce.Web.Services;
using NamEcommerce.Web.Services.Catalog;
using NamEcommerce.Web.Services.DeliveryNotes;
using NamEcommerce.Web.Services.Inventory;
using NamEcommerce.Web.Services.Orders;
using NamEcommerce.Web.Services.Preparations;
using NamEcommerce.Web.Services.PurchaseOrders;
using NamEcommerce.Web.Validators.Users;

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
    var appConfig = new AppConfig();
    configuration.GetSection(AppConstants.AppConfigSectionName).Bind(appConfig);

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
            opt => opt.MigrationsAssembly("NamEcommerce.Data.SqlServer"))
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

    services.AddScoped<IUserManager, UserManager>();
    services.AddScoped<ICategoryManager, CategoryManager>();
    services.AddScoped<IUnitMeasurementManager, UnitMeasurementManager>();
    services.AddScoped<IVendorManager, VendorManager>();
    services.AddScoped<IProductManager, ProductManager>();
    services.AddScoped<IPictureManager, PictureManager>();
    services.AddScoped<IWarehouseManager, WarehouseManager>();
    services.AddScoped<IInventoryStockManager, InventoryStockManager>();
    services.AddScoped<IInventoryValidator, InventoryValidator>();
    services.AddScoped<IStockAuditLogger, StockAuditLogger>();
    services.AddScoped<ICustomerManager, CustomerManager>();
    services.AddScoped<IExpenseManager, ExpenseManager>();
    services.AddScoped<IDeliveryNoteManager, DeliveryNoteManager>();
    services.AddScoped<IOrderManager, OrderManager>();
    services.AddScoped<ICustomerDebtManager, CustomerDebtManager>();

    services.AddScoped<ISecurityService, SecurityService>();
    services.AddScoped<IEventPublisher, EventPublisher>();

    services.AddScoped<ICategoryAppService, CategoryAppService>();
    services.AddScoped<IUnitMeasurementAppService, UnitMeasurementAppService>();
    services.AddScoped<IUserAppService, UserAppService>();
    services.AddScoped<IVendorAppService, VendorAppService>();
    services.AddScoped<IProductAppService, ProductAppService>();
    services.AddScoped<IPictureAppService, PictureAppService>();
    services.AddScoped<IInventoryAppService, InventoryAppService>();
    services.AddScoped<IWarehouseAppService, WarehouseAppService>();
    services.AddScoped<IPurchaseOrderManager, PurchaseOrderManager>();
    services.AddScoped<IPurchaseOrderAppService, PurchaseOrderAppService>();
    services.AddScoped<ICustomerAppService, CustomerAppService>();
    services.AddScoped<IFinancialReportAppService, FinancialReportAppService>();
    services.AddScoped<IExpenseAppService, ExpenseAppService>();
    services.AddScoped<IDeliveryNoteAppService, DeliveryNoteAppService>();
    services.AddScoped<IPreparationAppService, PreparationAppService>();
    services.AddScoped<IOrderAppService, OrderAppService>();
    services.AddScoped<ICustomerDebtAppService, CustomerDebtAppService>();

    builder.Services.AddHttpClient<IN8nAppService, N8nAppService>(client =>
    {
        client.BaseAddress = new Uri(appConfig.N8nEndpoint);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    services.AddScoped<IInformationService, InformationService>();
    services.AddScoped<ICurrentUserService, CurrentUserService>();
    services.AddScoped<IWebHelper, WebHelper>();

    services.AddScoped<ICategoryModelFactory, CategoryModelFactory>();
    services.AddScoped<IProductModelFactory, ProductModelFactory>();
    services.AddScoped<IWarehouseModelFactory, WarehouseModelFactory>();
    services.AddScoped<IPurchaseOrderModelFactory, PurchaseOrderModelFactory>();
    services.AddScoped<IOrderModelFactory, OrderModelFactory>();
    services.AddScoped<IPreparationModelFactory, PreparationModelFactory>();
    services.AddScoped<IDeliveryNoteModelFactory, DeliveryNoteModelFactory>();

    services.AddMediatR(config =>
    {
        config.RegisterServicesFromAssemblyContaining<CategoryAppService>();
        config.RegisterServicesFromAssemblyContaining<CookieAuthenticateUserHandler>();
    });

    services.AddSession();

    //mvc
    var mvcBuilder = services.AddMvc(options =>
    {
        options.ModelBinderProviders.Insert(0, new TrimModelBinderProvider());
        options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
    }).AddSessionStateTempDataProvider();

    if (builder.Environment.IsDevelopment())
    {
        mvcBuilder.AddRazorRuntimeCompilation();
    }

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

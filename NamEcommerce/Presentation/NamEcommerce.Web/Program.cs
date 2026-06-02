#region using

using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Communication;
using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Application.Contracts.Dashboard;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.Localizations;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Preparation;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Contracts.Report;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Application.Contracts.StockAdjustment;
using NamEcommerce.Application.Contracts.StockTransfer;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Application.Services.Catalog;
using NamEcommerce.Application.Services.Communication;
using NamEcommerce.Application.Services.CustomerPortal;
using NamEcommerce.Application.Services.Customers;
using NamEcommerce.Application.Services.Dashboard;
using NamEcommerce.Application.Services.Debts;
using NamEcommerce.Application.Services.DeliveryNotes;
using NamEcommerce.Application.Services.Finance;
using NamEcommerce.Application.Services.GoodsReceipts;
using NamEcommerce.Application.Services.Inventory;
using NamEcommerce.Application.Services.Media;
using NamEcommerce.Application.Services.Orders;
using NamEcommerce.Application.Services.Preparation;
using NamEcommerce.Application.Services.PurchaseOrders;
using NamEcommerce.Application.Services.Report;
using NamEcommerce.Application.Services.Returns;
using NamEcommerce.Application.Services.StockAdjustment;
using NamEcommerce.Application.Services.StockTransfer;
using NamEcommerce.Application.Services.Users;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Data.SqlServer;
using NamEcommerce.Data.SqlServer.Interceptors;
using NamEcommerce.Data.SqlServer.Outbox;
using NamEcommerce.Domain.Services.Catalog;
using NamEcommerce.Domain.Services.CustomerPortal;
using NamEcommerce.Domain.Services.Customers;
using NamEcommerce.Domain.Services.Debts;
using NamEcommerce.Domain.Services.DeliveryNotes;
using NamEcommerce.Domain.Services.Finance;
using NamEcommerce.Domain.Services.GoodsReceipts;
using NamEcommerce.Domain.Services.Inventory;
using NamEcommerce.Domain.Services.Media;
using NamEcommerce.Domain.Services.Orders;
using NamEcommerce.Domain.Services.PurchaseOrders;
using NamEcommerce.Domain.Services.Returns;
using NamEcommerce.Domain.Services.Security;
using NamEcommerce.Domain.Services.StockAdjustment;
using NamEcommerce.Domain.Services.StockTransfer;
using NamEcommerce.Domain.Services.Users;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Services.Catalog;
using NamEcommerce.Domain.Shared.Services.CustomerPortal;
using NamEcommerce.Domain.Shared.Services.Customers;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Finance;
using NamEcommerce.Domain.Shared.Services.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Media;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.Outbox;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.Returns;
using NamEcommerce.Domain.Shared.Services.Security;
using NamEcommerce.Domain.Shared.Services.StockAdjustment;
using NamEcommerce.Domain.Shared.Services.StockTransfer;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Domain.Shared.Settings;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Services;
using NamEcommerce.Web.Framework.Commands.Handlers.Users;
using NamEcommerce.Web.Framework.Services;
using NamEcommerce.Web.Mvc.Binders;
using NamEcommerce.Web.Mvc.Filters;
using NamEcommerce.Web.Services;
using NamEcommerce.Web.Services.Catalog;
using NamEcommerce.Web.Services.CustomerPortal;
using NamEcommerce.Web.Services.Dashboard;
using NamEcommerce.Web.Services.Debts;
using NamEcommerce.Web.Services.DeliveryNotes;
using NamEcommerce.Web.Services.GoodsReceipts;
using NamEcommerce.Web.Services.Inventory;
using NamEcommerce.Web.Services.Localizations;
using NamEcommerce.Web.Services.Notifications;
using NamEcommerce.Web.Services.Orders;
using NamEcommerce.Web.Services.Preparations;
using NamEcommerce.Web.Services.PurchaseOrders;
using NamEcommerce.Web.Services.Returns;
using NamEcommerce.Web.Services.StockAdjustment;
using NamEcommerce.Web.Services.StockTransfer;
using NamEcommerce.Web.Services.E2E;
using NamEcommerce.Web.Services.Users;
using NamEcommerce.Web.Validators.Users;
using System.Globalization;

#endregion

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
    services.Configure<AppConfig>(options => builder.Configuration.Bind(AppConstants.AppConfigSectionName, options));
    services.AddScoped(services => services.GetRequiredService<IOptionsSnapshot<AppConfig>>().Value);

    services.Configure<InfoOptions>(options => builder.Configuration.Bind(AppConstants.InfoSectionName, options));
    services.AddScoped(services => services.GetRequiredService<IOptionsSnapshot<InfoOptions>>().Value);

    services.Configure<CultureConfig>(options => builder.Configuration.Bind(AppConstants.CultureConfigSectionName, options));
    services.AddScoped(services => services.GetRequiredService<IOptionsSnapshot<CultureConfig>>().Value);

    services.Configure<WarehouseSettings>(options => builder.Configuration.Bind(AppConstants.WarehouseSettingSectionName, options));
    services.AddScoped(services => services.GetRequiredService<IOptionsSnapshot<WarehouseSettings>>().Value);

    var customerPortalSecurityOptions = new CustomerPortalSecurityOptions();
    configuration.GetSection(CustomerPortalSecurityOptions.SectionName).Bind(customerPortalSecurityOptions);
    services.AddSingleton(customerPortalSecurityOptions);
    ConfigureDataProtection(services, configuration);
    ConfigureForwardedHeaders(services);

    services.AddScoped<DomainEventDispatchInterceptor>();
    services.AddDbContext<NamEcommerceEfDbContext>((sp, opts) =>
    {
        opts.UseSqlServer(configuration.GetConnectionString(nameof(NamEcommerceEfDbContext)),
            opt => opt.MigrationsAssembly("NamEcommerce.Data.SqlServer"));
        opts.AddInterceptors(sp.GetRequiredService<DomainEventDispatchInterceptor>());

        if (builder.Environment.IsDevelopment())
            opts.EnableSensitiveDataLogging(true);

    });
    services.AddScoped<IDbContext, NamEcommerceEfDbContext>();
    services.AddScoped(typeof(IRepository<>), typeof(NamEcommerceEfRepository<>));
    services.AddScoped(typeof(IEntityDataReader<>), typeof(EntityDataReader<>));
    services.AddScoped(typeof(IGetByIdService<>), typeof(EntityDataReader<>));
    services.AddScoped<IOutbox, OutboxAccessor>();

    // Outbox processor (background service) — đọc OutboxMessages chưa processed và publish qua MediatR.
    services.Configure<OutboxProcessorOptions>(configuration.GetSection("Outbox"));
    services.AddHostedService<OutboxProcessor>();

    services.AddScoped<IUserManager, UserManager>();
    services.AddScoped<ICategoryManager, CategoryManager>();
    services.AddScoped<IUnitMeasurementManager, UnitMeasurementManager>();
    services.AddScoped<IVendorManager, VendorManager>();
    services.AddScoped<IProductManager, ProductManager>();
    services.AddScoped<IPictureManager, PictureManager>();
    services.AddScoped<IWarehouseManager, WarehouseManager>();
    services.AddScoped<InventoryStockManager>();
    services.AddScoped<IInventoryStockManager>(services => services.GetRequiredService<InventoryStockManager>());
    services.AddScoped<IInventoryCostingManager, InventoryCostingManager>();
    services.AddScoped<IProductReservationManager, ProductReservationManager>();
    services.AddScoped<IOrderItemChangeAuditManager, OrderItemChangeAuditManager>();
    services.AddScoped<IPurchaseOrderItemChangeAuditManager, PurchaseOrderItemChangeAuditManager>();
    services.AddScoped<IShortageQueryService, ShortageQueryService>();
    services.AddScoped<IInventoryValidator, InventoryValidator>();
    services.AddScoped<IStockAuditLogger, StockAuditLogger>();
    services.AddScoped<ICustomerManager, CustomerManager>();
    services.AddScoped<IExpenseManager, ExpenseManager>();
    services.AddScoped<IDeliveryNoteManager, DeliveryNoteManager>();
    services.AddScoped<IOrderManager, OrderManager>();
    services.AddScoped<ICustomerDebtManager, CustomerDebtManager>();
    services.AddScoped<ICustomerPortalSecurityManager, CustomerPortalSecurityManager>();
    services.AddScoped<ICustomerPortalManager, CustomerPortalManager>();
    services.AddScoped<IVendorDebtManager, VendorDebtManager>();
    services.AddScoped<IGoodsReceiptManager, GoodsReceiptManager>();
    services.AddScoped<ICustomerReturnManager, CustomerReturnManager>();
    services.AddScoped<IVendorReturnManager, VendorReturnManager>();
    services.AddScoped<ICustomerRefundManager, CustomerRefundManager>();
    services.AddScoped<IStockAdjustmentNoteManager, StockAdjustmentNoteManager>();
    services.AddScoped<IStockTransferNoteManager, StockTransferNoteManager>();

    services.AddScoped<ISecurityService, SecurityService>();
    services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

    services.AddScoped<ICategoryAppService, CategoryAppService>();
    services.AddScoped<IUnitMeasurementAppService, UnitMeasurementAppService>();
    services.AddScoped<IUserAppService, UserAppService>();
    services.AddScoped<IVendorAppService, VendorAppService>();
    services.AddScoped<IProductAppService, ProductAppService>();
    services.AddScoped<IPictureAppService, PictureAppService>();
    services.AddScoped<IInventoryAppService, InventoryAppService>();
    services.AddScoped<IInventoryCostingAppService, InventoryCostingAppService>();
    services.AddScoped<IWarehouseAppService, WarehouseAppService>();
    services.AddScoped<IPurchaseOrderManager, PurchaseOrderManager>();
    services.AddScoped<IPurchaseOrderAllocationManager, PurchaseOrderAllocationManager>();
    services.AddScoped<IDirectShipManager, DirectShipManager>();
    services.AddScoped<IDirectShipAppService, DirectShipAppService>();
    services.AddScoped<ISupplierSuggestionService, SupplierSuggestionService>();
    services.AddScoped<IPurchaseOrderAppService, PurchaseOrderAppService>();
    services.AddScoped<IPurchaseOrderAuditAppService, PurchaseOrderAuditAppService>();
    services.AddScoped<IShortageAggregationAppService, ShortageAggregationAppService>();
    services.AddScoped<ICustomerAppService, CustomerAppService>();
    services.AddScoped<IDashboardAppService, DashboardAppService>();
    services.AddScoped<IFinancialReportAppService, FinancialReportAppService>();
    services.AddScoped<IDirectShipReportAppService, DirectShipReportAppService>();
    services.AddScoped<IExpenseAppService, ExpenseAppService>();
    services.AddScoped<IDeliveryNoteAppService, DeliveryNoteAppService>();
    services.AddScoped<IPreparationAppService, PreparationAppService>();
    services.AddScoped<IOrderAppService, OrderAppService>();
    services.AddScoped<IOrderAuditAppService, OrderAuditAppService>();
    services.AddScoped<ICustomerDebtAppService, CustomerDebtAppService>();
    services.AddScoped<ICustomerPortalAdminAppService, CustomerPortalAdminAppService>();
    services.AddScoped<ICustomerPortalDeliveryTokenAppService, CustomerPortalDeliveryTokenAppService>();
    services.AddScoped<ICustomerPortalNotificationSender, MockSmsCustomerPortalNotificationSender>();
    services.AddScoped<ICustomerPortalNotificationSender, MockEmailCustomerPortalNotificationSender>();
    services.AddScoped<IVendorDebtAppService, VendorDebtAppService>();
    services.AddScoped<IGoodsReceiptAppService, GoodsReceiptAppService>();
    services.AddScoped<ICustomerReturnAppService, CustomerReturnAppService>();
    services.AddScoped<IVendorReturnAppService, VendorReturnAppService>();
    services.AddScoped<ILocalizationAppService, LocalizationService>();
    services.AddScoped<ICustomerRefundAppService, CustomerRefundAppService>();
    services.AddScoped<IStockAdjustmentNoteAppService, StockAdjustmentNoteAppService>();
    services.AddScoped<IStockTransferNoteAppService, StockTransferNoteAppService>();

    builder.Services.AddHttpClient<IN8nAppService, N8nAppService>(client =>
    {
        client.BaseAddress = new Uri(appConfig.N8nEndpoint);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    services.AddScoped<IInformationService, InformationService>();
    services.AddScoped<ICurrentUserService, CurrentUserService>();
    services.AddScoped<IWebHelper, WebHelper>();
    services.AddScoped<ICustomerPortalQrCodeService, CustomerPortalQrCodeService>();
    services.AddScoped<INotificationService, TempDataNotificationService>();

    services.AddScoped<ICategoryModelFactory, CategoryModelFactory>();
    services.AddScoped<IProductModelFactory, ProductModelFactory>();
    services.AddScoped<IDashboardModelFactory, DashboardModelFactory>();
    services.AddScoped<IWarehouseModelFactory, WarehouseModelFactory>();
    services.AddScoped<IPurchaseOrderModelFactory, PurchaseOrderModelFactory>();
    services.AddScoped<IOrderModelFactory, OrderModelFactory>();
    services.AddScoped<IPreparationModelFactory, PreparationModelFactory>();
    services.AddScoped<IDeliveryNoteModelFactory, DeliveryNoteModelFactory>();
    services.AddScoped<IGoodsReceiptModelFactory, GoodsReceiptModelFactory>();
    services.AddScoped<ICustomerReturnModelFactory, CustomerReturnModelFactory>();
    services.AddScoped<IVendorReturnModelFactory, VendorReturnModelFactory>();
    services.AddScoped<ICustomerRefundModelFactory, CustomerRefundModelFactory>();
    services.AddScoped<IStockAdjustmentNoteModelFactory, StockAdjustmentNoteModelFactory>();
    services.AddScoped<IStockTransferNoteModelFactory, StockTransferNoteModelFactory>();

    if (builder.Environment.IsEnvironment("E2E"))
    {
        services.AddScoped<IE2ETestDataService, E2ETestDataService>();
    }

    services.AddMediatR(config =>
    {
        config.RegisterServicesFromAssemblyContaining<CategoryAppService>();
        config.RegisterServicesFromAssemblyContaining<CookieAuthenticateUserHandler>();
    });

    services.AddLocalization();

    services.AddAuthentication(opts =>
        opts.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme
    ).AddCookie(opts =>
    {
        opts.LoginPath = "/User/Login";
        opts.LogoutPath = "/User/Logout";
    });

    services.AddHttpContextAccessor();

    services.AddSession();

    services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
    services.AddValidatorsFromAssemblyContaining<LoginModelValidator>();

    var mvcBuilder = services.AddMvc(options =>
    {
        options.Filters.Add<GlobalExceptionFilter>();
        options.ModelBinderProviders.Insert(0, new TrimModelBinderProvider());
        options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
    });
    mvcBuilder.AddSessionStateTempDataProvider();
    if (builder.Environment.IsDevelopment())
    {
        mvcBuilder.AddRazorRuntimeCompilation();
    }
}

void ConfigureDataProtection(IServiceCollection services, ConfigurationManager configuration)
{
    var dataProtectionBuilder = services.AddDataProtection()
        .SetApplicationName(configuration["DataProtection:ApplicationName"] ?? "NamEcommerce.Web");

    var keysPath = configuration["DataProtection:KeysPath"];
    if (!string.IsNullOrWhiteSpace(keysPath))
        dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

void ConfigureForwardedHeaders(IServiceCollection services)
{
    services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
    });
}

void Configure(WebApplication app)
{
    if (app.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
        app.UseForwardedHeaders();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    var cultureConfig = app.Services.GetRequiredService<IOptions<CultureConfig>>().Value;
    var supportedCultures = cultureConfig.SupportedCultures
        .Select(c => new CultureInfo(c))
        .ToList();
    app.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture(cultureConfig.DefaultCulture),
        SupportedCultures = supportedCultures,
        SupportedUICultures = supportedCultures,
    });

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseSession();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
}

#endregion

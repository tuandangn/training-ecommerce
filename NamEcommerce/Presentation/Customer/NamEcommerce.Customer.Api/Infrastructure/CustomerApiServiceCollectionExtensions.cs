using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Application.Services.CustomerPortal;
using NamEcommerce.Application.Services.Debts;
using NamEcommerce.Application.Services.DeliveryNotes;
using NamEcommerce.Application.Services.Inventory;
using NamEcommerce.Application.Services.Media;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Data.SqlServer;
using NamEcommerce.Domain.Services.CustomerPortal;
using NamEcommerce.Domain.Services.Debts;
using NamEcommerce.Domain.Services.DeliveryNotes;
using NamEcommerce.Domain.Services.Finance;
using NamEcommerce.Domain.Services.Inventory;
using NamEcommerce.Domain.Services.Media;
using NamEcommerce.Domain.Services.Orders;
using NamEcommerce.Domain.Services.Returns;
using NamEcommerce.Domain.Services.Security;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Services.CustomerPortal;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Finance;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Media;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.Returns;
using NamEcommerce.Domain.Shared.Services.Security;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Customer.Framework.Commands.Handlers;
using NamEcommerce.Customer.Framework.Services;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Services.Inventory;

namespace NamEcommerce.Customer.Api.Infrastructure;

internal static class CustomerApiServiceCollectionExtensions
{
    internal static IServiceCollection AddCustomerPortalApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddHttpContextAccessor();
        services.AddSingleton(BuildCustomerPortalSecurityOptions(configuration));
        services.AddSingleton(BuildCustomerPortalStoreOptions(configuration));
        services.AddCustomerPortalDataProtection(configuration);
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
        });

        services.AddCors(options =>
        {
            var configuredOrigins = configuration.GetSection("CustomerPortal:AllowedOrigins")
                .GetChildren()
                .Select(origin => origin.Value)
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => origin!)
                .ToArray();
            var allowedOrigins = configuredOrigins.Length > 0
                ? configuredOrigins
                : new[]
                {
                    "http://localhost:5173",
                    "https://localhost:5173",
                    "http://127.0.0.1:5173",
                    "https://127.0.0.1:5173"
                };

            options.AddPolicy("CustomerClient", builder =>
            {
                builder.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("CustomerOtp", limiter =>
            {
                limiter.PermitLimit = 5;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });
            options.AddFixedWindowLimiter("CustomerPublic", limiter =>
            {
                limiter.PermitLimit = 60;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiter.QueueLimit = 0;
            });
        });

        services.AddDbContext<NamEcommerceEfDbContext>(opts =>
        {
            opts.UseSqlServer(
                configuration.GetConnectionString(nameof(NamEcommerceEfDbContext)),
                sql => sql.MigrationsAssembly("NamEcommerce.Data.SqlServer"));
        });

        services.AddScoped<IDbContext, NamEcommerceEfDbContext>();
        services.AddScoped(typeof(IRepository<>), typeof(NamEcommerceEfRepository<>));
        services.AddScoped(typeof(IEntityDataReader<>), typeof(EntityDataReader<>));
        services.AddScoped(typeof(IGetByIdService<>), typeof(EntityDataReader<>));

        services.AddScoped<ICurrentUserAccessor, CustomerApiCurrentUserAccessor>();
        services.AddScoped<ICustomerSessionAccessor, CustomerSessionAccessor>();

        services.AddScoped<IStockAuditLogger, StockAuditLogger>();
        services.AddScoped<IWarehouseManager, WarehouseManager>();
        services.AddScoped<InventoryStockManager>();
        services.AddScoped<IInventoryStockManager>(sp => sp.GetRequiredService<InventoryStockManager>());
        services.AddScoped<IProductReservationManager, ProductReservationManager>();
        services.AddScoped<IOrderManager, OrderManager>();
        services.AddScoped<IExpenseManager, ExpenseManager>();
        services.AddScoped<ICustomerDebtManager, CustomerDebtManager>();
        services.AddScoped<ICustomerReturnManager, CustomerReturnManager>();
        services.AddScoped<IWarehouseManager, WarehouseManager>();
        services.AddScoped<IDeliveryNoteManager, DeliveryNoteManager>();
        services.AddScoped<IPictureManager, PictureManager>();
        services.AddScoped<ICustomerPortalSecurityManager, CustomerPortalSecurityManager>();
        services.AddScoped<ICustomerPortalManager, CustomerPortalManager>();
        services.AddScoped<ISecurityService, SecurityService>();

        services.AddScoped<IDeliveryNoteAppService, DeliveryNoteAppService>();
        services.AddScoped<IWarehouseAppService, WarehouseAppService>();
        services.AddScoped<IPictureAppService, PictureAppService>();
        services.AddScoped<ICustomerDebtAppService, CustomerDebtAppService>();
        services.AddScoped<ICustomerPortalAuthAppService, CustomerPortalAuthAppService>();
        services.AddScoped<ICustomerPortalAppService, CustomerPortalAppService>();
        services.AddScoped<IWarehouseAppService, WarehouseAppService>();
        services.AddScoped<ICustomerPortalPaymentAppService, CustomerPortalPaymentAppService>();
        services.AddScoped<ICustomerOtpSender, MockSmsOtpSender>();
        services.AddScoped<ICustomerOtpSender, MockEmailOtpSender>();
        services.AddScoped<ICustomerPaymentProvider, MockCustomerPaymentProvider>();

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<CustomerPortalCommandHandlers>();
        });

        return services;
    }

    private static CustomerPortalSecurityOptions BuildCustomerPortalSecurityOptions(IConfiguration configuration)
    {
        var options = new CustomerPortalSecurityOptions();
        configuration.GetSection(CustomerPortalSecurityOptions.SectionName).Bind(options);
        return options;
    }

    private static CustomerPortalStoreOptions BuildCustomerPortalStoreOptions(IConfiguration configuration)
    {
        var options = new CustomerPortalStoreOptions();
        configuration.GetSection(CustomerPortalStoreOptions.SectionName).Bind(options);
        return options;
    }

    private static IServiceCollection AddCustomerPortalDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddDataProtection()
            .SetApplicationName(configuration["CustomerPortal:DataProtection:ApplicationName"] ?? "NamEcommerce.CustomerPortal");

        var keysPath = configuration["CustomerPortal:DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
            builder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        return services;
    }
}

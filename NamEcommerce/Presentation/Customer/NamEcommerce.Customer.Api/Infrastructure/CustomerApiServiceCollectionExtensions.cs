using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Services.CustomerPortal;
using NamEcommerce.Application.Services.Debts;
using NamEcommerce.Application.Services.DeliveryNotes;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Data.SqlServer;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Services.CustomerPortal;
using NamEcommerce.Domain.Services.Debts;
using NamEcommerce.Domain.Services.DeliveryNotes;
using NamEcommerce.Domain.Services.Finance;
using NamEcommerce.Domain.Services.Inventory;
using NamEcommerce.Domain.Services.Orders;
using NamEcommerce.Domain.Services.Returns;
using NamEcommerce.Domain.Services.Security;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Services.CustomerPortal;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Finance;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.Returns;
using NamEcommerce.Domain.Shared.Services.Security;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Customer.Framework.Commands.Handlers;
using NamEcommerce.Customer.Framework.Services;

namespace NamEcommerce.Customer.Api.Infrastructure;

internal static class CustomerApiServiceCollectionExtensions
{
    internal static IServiceCollection AddCustomerPortalApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddHttpContextAccessor();

        services.AddCors(options =>
        {
            options.AddPolicy("CustomerClient", builder =>
            {
                builder.WithOrigins("http://localhost:5173", "https://localhost:5173")
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
        services.AddScoped<InventoryStockManager>();
        services.AddScoped<IInventoryStockManager>(sp => sp.GetRequiredService<InventoryStockManager>());
        services.AddScoped<IProductReservationManager, ProductReservationManager>();
        services.AddScoped<IOrderManager, OrderManager>();
        services.AddScoped<IExpenseManager, ExpenseManager>();
        services.AddScoped<ICustomerDebtManager, CustomerDebtManager>();
        services.AddScoped<ICustomerReturnManager, CustomerReturnManager>();
        services.AddScoped<IDeliveryNoteManager, DeliveryNoteManager>();
        services.AddScoped<ICustomerPortalSecurityManager, CustomerPortalSecurityManager>();
        services.AddScoped<ICustomerPortalManager, CustomerPortalManager>();
        services.AddScoped<ISecurityService, SecurityService>();

        services.AddScoped<IDeliveryNoteAppService, DeliveryNoteAppService>();
        services.AddScoped<ICustomerDebtAppService, CustomerDebtAppService>();
        services.AddScoped<ICustomerPortalAuthAppService, CustomerPortalAuthAppService>();
        services.AddScoped<ICustomerPortalAppService, CustomerPortalAppService>();
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
}

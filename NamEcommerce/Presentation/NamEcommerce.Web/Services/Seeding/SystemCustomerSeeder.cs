using NamEcommerce.Application.Contracts.Customers;

namespace NamEcommerce.Web.Services.Seeding;

public sealed class SystemCustomerSeeder(ICustomerAppService customerAppService) : IDataSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken = default)
        => customerAppService.GetOrCreateRetailWalkInCustomerAsync();
}

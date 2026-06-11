using NamEcommerce.Data.Contracts;

namespace NamEcommerce.Web.Services.Seeding;

public sealed class DataSeederRunner(IEnumerable<IDataSeeder> seeders, IUnitOfWork unitOfWork, ILogger<DataSeederRunner> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        foreach (var seeder in seeders)
        {
            var name = seeder.GetType().Name;
            try
            {
                await seeder.SeedAsync(cancellationToken).ConfigureAwait(false);
                await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Seeder {Seeder} completed.", name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Seeder {Seeder} failed.", name);
                throw;
            }
        }
    }
}

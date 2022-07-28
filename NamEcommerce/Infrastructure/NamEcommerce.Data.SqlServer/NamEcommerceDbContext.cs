namespace NamEcommerce.Data.SqlServer;

public sealed class NamEcommerceDbContext : DbContext
{
    public NamEcommerceDbContext(DbContextOptions<NamEcommerceDbContext> opts) : base(opts)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NamEcommerceDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

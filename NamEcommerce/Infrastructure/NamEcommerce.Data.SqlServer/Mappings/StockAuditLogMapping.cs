namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class StockAuditLogMapping : IEntityTypeConfiguration<StockAuditLog>
{
    public void Configure(EntityTypeBuilder<StockAuditLog> builder)
    {
        builder.ToTable(nameof(StockAuditLog), DbScheme);
        builder.HasKey(d => d.Id);
    }
}

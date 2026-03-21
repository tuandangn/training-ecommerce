namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class UnitMeasurementMapping : IEntityTypeConfiguration<UnitMeasurement>
{
    public void Configure(EntityTypeBuilder<UnitMeasurement> builder)
    {
        builder.ToTable(nameof(UnitMeasurement), DbScheme);
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.NormalizedName).HasMaxLength(400);
    }
}

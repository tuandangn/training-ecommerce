namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class DirectShipAddressChangeLogMapping : IEntityTypeConfiguration<DirectShipAddressChangeLog>
{
    public void Configure(EntityTypeBuilder<DirectShipAddressChangeLog> builder)
    {
        builder.ToTable(nameof(DirectShipAddressChangeLog), DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AllocationId).IsRequired();
        builder.HasIndex(x => x.AllocationId);

        builder.Property(x => x.OldAddress).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NewAddress).HasMaxLength(500).IsRequired();
        builder.Property(x => x.OldContactName).HasMaxLength(200).IsRequired(false);
        builder.Property(x => x.NewContactName).HasMaxLength(200).IsRequired(false);
        builder.Property(x => x.OldContactPhone).HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.NewContactPhone).HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.EditedByUserId).IsRequired();
        builder.Property(x => x.EditedAtUtc).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired(false);

        builder.HasOne<PurchaseOrderItemAllocation>()
            .WithMany()
            .HasForeignKey(x => x.AllocationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

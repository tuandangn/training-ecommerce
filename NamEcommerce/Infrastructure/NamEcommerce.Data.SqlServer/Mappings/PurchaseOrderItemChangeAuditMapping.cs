namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class PurchaseOrderItemChangeAuditMapping : IEntityTypeConfiguration<PurchaseOrderItemChangeAudit>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItemChangeAudit> builder)
    {
        builder.ToTable(nameof(PurchaseOrderItemChangeAudit), DbScheme);
        builder.HasKey(audit => audit.Id);

        builder.Property(audit => audit.ProductName).HasMaxLength(1000).IsRequired();
        builder.Property(audit => audit.Action).HasConversion<int>().IsRequired();
        builder.Property(audit => audit.OldQuantity).HasColumnType("decimal(18,2)").IsRequired(false);
        builder.Property(audit => audit.NewQuantity).HasColumnType("decimal(18,2)").IsRequired(false);
        builder.Property(audit => audit.OldUnitCost).HasColumnType("decimal(18,2)").IsRequired(false);
        builder.Property(audit => audit.NewUnitCost).HasColumnType("decimal(18,2)").IsRequired(false);
        builder.Property(audit => audit.OldNote).HasMaxLength(2000).IsRequired(false);
        builder.Property(audit => audit.NewNote).HasMaxLength(2000).IsRequired(false);
        builder.Property(audit => audit.ChangedByUsername).HasMaxLength(1000).IsRequired(false);
        builder.Property(audit => audit.CreatedOnUtc).IsRequired();

        builder.HasIndex(audit => audit.PurchaseOrderId);
        builder.HasIndex(audit => new { audit.PurchaseOrderId, audit.CreatedOnUtc });
        builder.HasIndex(audit => audit.PurchaseOrderItemId);

        builder.HasOne<PurchaseOrder>().WithMany().HasForeignKey(audit => audit.PurchaseOrderId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Product>().WithMany().HasForeignKey(audit => audit.ProductId).OnDelete(DeleteBehavior.NoAction);
    }
}

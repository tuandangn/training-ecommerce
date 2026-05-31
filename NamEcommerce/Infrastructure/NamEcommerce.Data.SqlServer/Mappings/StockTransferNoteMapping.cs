using NamEcommerce.Domain.Entities.StockTransfer;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class StockTransferNoteMapping : IEntityTypeConfiguration<StockTransferNote>
{
    public void Configure(EntityTypeBuilder<StockTransferNote> builder)
    {
        builder.ToTable(nameof(StockTransferNote), DbScheme);
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(n => n.Code).IsUnique();

        builder.Property(n => n.FromWarehouseId).IsRequired();
        builder.Property(n => n.FromWarehouseName).HasMaxLength(500);
        builder.Property(n => n.ToWarehouseId).IsRequired();
        builder.Property(n => n.ToWarehouseName).HasMaxLength(500);
        builder.Property(n => n.Note).HasMaxLength(2000);

        builder.Property(n => n.Status).IsRequired().HasConversion<int>();
        builder.Property(n => n.ApprovedOnUtc).IsRequired(false);
        builder.Property(n => n.CreatedByUserId).IsRequired(false);
        builder.Property(n => n.CreatedOnUtc).IsRequired();
        builder.Property(n => n.UpdatedOnUtc).IsRequired(false);

        builder.HasIndex(n => new { n.FromWarehouseId, n.Status });
        builder.HasIndex(n => n.CreatedOnUtc);

        builder.HasMany(n => n.Items)
            .WithOne()
            .HasForeignKey(i => i.NoteId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(n => n.Items).AutoInclude();
        builder.Metadata.FindNavigation(nameof(StockTransferNote.Items))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class StockTransferNoteItemMapping : IEntityTypeConfiguration<StockTransferNoteItem>
{
    public void Configure(EntityTypeBuilder<StockTransferNoteItem> builder)
    {
        builder.ToTable(nameof(StockTransferNoteItem), DbScheme);
        builder.HasKey(i => i.Id);

        builder.Property(i => i.NoteId).IsRequired();
        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(500);
        builder.Property(i => i.Quantity).IsRequired().HasPrecision(18, 4);
        builder.Property(i => i.UnitCost).IsRequired().HasPrecision(18, 4);

        builder.HasIndex(i => new { i.NoteId, i.ProductId });
    }
}

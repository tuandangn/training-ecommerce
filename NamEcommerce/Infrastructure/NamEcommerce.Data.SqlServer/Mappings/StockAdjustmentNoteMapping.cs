using NamEcommerce.Domain.Entities.StockAdjustment;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class StockAdjustmentNoteMapping : IEntityTypeConfiguration<StockAdjustmentNote>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentNote> builder)
    {
        builder.ToTable(nameof(StockAdjustmentNote), DbScheme);
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(n => n.Code).IsUnique();

        builder.Property(n => n.WarehouseId).IsRequired();
        builder.Property(n => n.WarehouseName).HasMaxLength(500);
        builder.Property(n => n.Note).HasMaxLength(2000);

        builder.Property(n => n.Status).IsRequired().HasConversion<int>();
        builder.Property(n => n.ApprovedOnUtc).IsRequired(false);
        builder.Property(n => n.CreatedByUserId).IsRequired(false);
        builder.Property(n => n.CreatedOnUtc).IsRequired();
        builder.Property(n => n.UpdatedOnUtc).IsRequired(false);

        builder.HasIndex(n => new { n.WarehouseId, n.Status });
        builder.HasIndex(n => n.CreatedOnUtc);

        builder.Metadata.FindNavigation(nameof(StockAdjustmentNote.Items))
            ?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(n => n.Items)
            .WithOne()
            .HasForeignKey(i => i.NoteId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(n => n.Items).AutoInclude();
    }
}

public sealed class StockAdjustmentNoteItemMapping : IEntityTypeConfiguration<StockAdjustmentNoteItem>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentNoteItem> builder)
    {
        builder.ToTable(nameof(StockAdjustmentNoteItem), DbScheme);
        builder.HasKey(i => i.Id);

        builder.Property(i => i.NoteId).IsRequired();
        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(500);
        builder.Property(i => i.SystemQuantity).IsRequired().HasPrecision(18, 4);
        builder.Property(i => i.PhysicalQuantity).IsRequired().HasPrecision(18, 4);

        // Delta là computed property — không map xuống DB
        builder.Ignore(i => i.Delta);

        builder.HasIndex(i => new { i.NoteId, i.ProductId });
    }
}

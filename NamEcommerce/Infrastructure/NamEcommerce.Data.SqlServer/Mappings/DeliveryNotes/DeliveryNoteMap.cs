using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NamEcommerce.Domain.Entities.DeliveryNotes;

namespace NamEcommerce.Data.SqlServer.Mappings.DeliveryNotes;

public class DeliveryNoteMap : IEntityTypeConfiguration<DeliveryNote>
{
    public void Configure(EntityTypeBuilder<DeliveryNote> builder)
    {
        builder.ToTable("DeliveryNote");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code).HasMaxLength(100).IsRequired();
        builder.HasIndex(d => d.Code).IsUnique();

        builder.Property(d => d.OrderId).IsRequired();
        builder.Property(d => d.CustomerId).IsRequired();
        builder.Property(d => d.CustomerName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.CustomerPhone).HasMaxLength(50).IsRequired(false);
        builder.Property(d => d.CustomerAddress).HasMaxLength(1000).IsRequired(false);
        
        builder.Property(d => d.ShippingAddress).HasMaxLength(1000).IsRequired();
        
        builder.Property(d => d.ShowPrice).IsRequired().HasDefaultValue(false);
        builder.Property(d => d.Note).HasMaxLength(2000).IsRequired(false);
        
        builder.Property(d => d.Status).IsRequired();
        
        builder.Property(d => d.DeliveredOnUtc).IsRequired(false);
        builder.Property(d => d.DeliveryProofPictureId).IsRequired(false);
        builder.Property(d => d.DeliveryReceiverName).HasMaxLength(255).IsRequired(false);
        
        builder.Property(d => d.CreatedByUserId).IsRequired(false);
        builder.Property(d => d.CreatedOnUtc).IsRequired();
        builder.Property(d => d.UpdatedOnUtc).IsRequired(false);

        // One-to-many relationship with items
        builder.Metadata.FindNavigation(nameof(DeliveryNote.Items))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(d => d.Items).WithOne().HasForeignKey(i => i.DeliveryNoteId).OnDelete(DeleteBehavior.Cascade);

        // Ignore computed property
        builder.Ignore(d => d.TotalAmount);
    }
}

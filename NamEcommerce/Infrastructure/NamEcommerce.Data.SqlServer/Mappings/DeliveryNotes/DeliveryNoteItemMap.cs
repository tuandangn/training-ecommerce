using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NamEcommerce.Domain.Entities.DeliveryNotes;

namespace NamEcommerce.Data.SqlServer.Mappings.DeliveryNotes;

public class DeliveryNoteItemMap : IEntityTypeConfiguration<DeliveryNoteItem>
{
    public void Configure(EntityTypeBuilder<DeliveryNoteItem> builder)
    {
        builder.ToTable("DeliveryNoteItem");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DeliveryNoteId).IsRequired();
        builder.Property(d => d.OrderItemId).IsRequired();
        builder.Property(d => d.ProductId).IsRequired();
        
        builder.Property(d => d.ProductName).HasMaxLength(400).IsRequired();
        
        builder.Property(d => d.Quantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(d => d.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();

        // Ignore computed property
        builder.Ignore(d => d.SubTotal);
    }
}

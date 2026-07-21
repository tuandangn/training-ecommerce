using NamEcommerce.Domain.Entities.DeliveryNotes;

namespace NamEcommerce.Data.SqlServer.Mappings;

public class DeliveryNoteSettlementItemMap : IEntityTypeConfiguration<DeliveryNoteSettlementItem>
{
    public void Configure(EntityTypeBuilder<DeliveryNoteSettlementItem> builder)
    {
        builder.ToTable(nameof(DeliveryNoteSettlementItem), DbScheme);
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DeliveryNoteId).IsRequired();
        builder.Property(d => d.DeliveryNoteItemId).IsRequired();
        builder.Property(d => d.AcceptedQuantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(d => d.RejectedQuantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(d => d.RejectReason).HasMaxLength(500).IsRequired(false);
    }
}

using NamEcommerce.Domain.Entities.DeliveryNotes;
using System.Linq;
using NamEcommerce.Domain.Metadata;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Values;

namespace NamEcommerce.Data.SqlServer.Mappings;

public class DeliveryNoteMap : IEntityTypeConfiguration<DeliveryNote>
{
    public void Configure(EntityTypeBuilder<DeliveryNote> builder)
    {
        builder.ToTable(nameof(DeliveryNote), DbScheme);

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code).HasMaxLength(100).IsRequired();
        builder.HasIndex(d => d.Code).IsUnique();

        builder.Property(d => d.OrderId).IsRequired();
        builder.Property(d => d.WarehouseId).IsRequired();
        builder.Property(d => d.OrderCode);
        builder.Property(d => d.AssignedDeliveryUserId).IsRequired(false);
        builder.Property(d => d.AssignedDeliveryUsername).HasMaxLength(200).IsRequired(false);
        builder.Property(d => d.AssignedDeliveryFullName).HasMaxLength(200).IsRequired(false);
        builder.Property(d => d.AssignedDeliveryOnUtc).IsRequired(false);
        builder.HasIndex(d => d.AssignedDeliveryUserId);

        builder.Property(d => d.SourceType)
            .IsRequired()
            .HasDefaultValue(DeliveryNoteSourceType.ToCustomer)
            .HasConversion<int>();

        builder.Property(d => d.CustomerId).IsRequired();
        builder.ComplexProperty(o => o.CustomerInfo, customerInfoBuilder =>
        {
            customerInfoBuilder.Property(info => info.FullName)
                .HasColumnName($"CustomerName")
                .HasMaxLength(200)
                .HasConversion(
                    info => info.Value,
                    value => new NormalizableString(value)
                );
            customerInfoBuilder.Property(info => info.Address)
                .HasColumnName($"Customer{nameof(CustomerInfo.Address)}")
                .HasMaxLength(500)
                .HasConversion(
                    info => info.Value,
                    value => new NormalizableString(value)
                );
            customerInfoBuilder.Property(c => c.PhoneNumber)
                .HasColumnName("CustomerPhone")
                .HasMaxLength(50)
                .IsRequired();
        });
        builder.ComplexProperty(c => c.ShippingAddress, shippingAddressProp =>
        {
            shippingAddressProp.Property(n => n.Value)
                          .HasColumnName(nameof(Order.ShippingAddress))
                          .HasMaxLength(1000)
                          .IsRequired();
            shippingAddressProp.Property(n => n.NormalizedValue)
                          .HasColumnName($"Normalized{nameof(Order.ShippingAddress)}")
                          .HasMaxLength(1000)
                          .IsRequired();
        });

        builder.Property(d => d.ShowPrice).IsRequired().HasDefaultValue(false);
        builder.Property(d => d.Note).HasMaxLength(2000).IsRequired(false);
        
        builder.Property(d => d.Status).IsRequired();
        
        builder.Property(d => d.DeliveredOnUtc).IsRequired(false);
        builder.Property(d => d.DeliveryProofPictureId).IsRequired(false);
        builder.Property(d => d.DeliveryProofPictureIds)
            .HasConversion(
                v => string.Join(',', v),
                v => string.IsNullOrEmpty(v)
                    ? (IReadOnlyCollection<Guid>)Array.Empty<Guid>()
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(Guid.Parse).ToList().AsReadOnly())
            .HasColumnName("DeliveryProofPictureIds")
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);
        builder.Property(d => d.DeliveryReceiverName).HasMaxLength(255).IsRequired(false);
        builder.Property(d => d.DeliveryLatitude).IsRequired(false);
        builder.Property(d => d.DeliveryLongitude).IsRequired(false);
        builder.Property(d => d.DeliveryLocationAddress).HasMaxLength(1000).IsRequired(false);
        builder.Property(d => d.DeliveryCompletionNote).HasMaxLength(2000).IsRequired(false);
        builder.Property(d => d.DeliveryCompletionSource).HasMaxLength(100).IsRequired(false);
        builder.Property(d => d.DeliveryCompletionIdempotencyKey).HasMaxLength(100).IsRequired(false);
        builder.Property(d => d.DeliveryCashCollectedAmount).HasColumnType("decimal(18,2)").IsRequired(false);
        builder.HasIndex(d => d.DeliveryCompletionIdempotencyKey)
            .IsUnique()
            .HasFilter("[DeliveryCompletionIdempotencyKey] IS NOT NULL");

        builder.Property(d => d.IsDirectShip).IsRequired().HasDefaultValue(false);
        builder.Property(d => d.DeliveryConfirmationStatus)
            .IsRequired()
            .HasDefaultValue(DeliveryConfirmationStatus.NotApplicable)
            .HasConversion<int>();
        builder.Property(d => d.ConfirmedAtUtc).IsRequired(false);
        builder.Property(d => d.ConfirmedNote).HasMaxLength(2000).IsRequired(false);
        builder.Property(d => d.SourceGoodsReceiptId).IsRequired(false);

        builder.Property(d => d.CreatedByUserId).IsRequired(false);
        builder.Property(d => d.CreatedOnUtc).IsRequired();
        builder.Property(d => d.UpdatedOnUtc).IsRequired(false);

        builder.HasMany(d => d.Items).WithOne().HasForeignKey(i => i.DeliveryNoteId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(d => d.Items).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();

        builder.Property(d => d.InvoiceNumber).HasMaxLength(20).IsRequired(false);
        builder.Property(d => d.InvoiceSeries).HasMaxLength(10).IsRequired(false);
        builder.Property(d => d.InvoiceDate).IsRequired(false);

        // Computed từ items — không persist
        builder.Ignore(d => d.TotalAmount);
        builder.Ignore(d => d.TotalDiscountAmount);
        builder.Ignore(d => d.TotalTaxAmount);
    }
}

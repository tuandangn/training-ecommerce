using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class DeliveryRunMap : IEntityTypeConfiguration<DeliveryRun>
{
    public void Configure(EntityTypeBuilder<DeliveryRun> builder)
    {
        builder.ToTable(nameof(DeliveryRun), DbScheme);
        builder.HasKey(run => run.Id);

        builder.Property(run => run.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(run => run.Code).IsUnique();

        builder.Property(run => run.AssignedDeliveryUserId).IsRequired();
        builder.Property(run => run.AssignedDeliveryUsername).IsRequired().HasMaxLength(200);
        builder.Property(run => run.AssignedDeliveryFullName).IsRequired().HasMaxLength(200);
        builder.Property(run => run.Status)
            .IsRequired()
            .HasDefaultValue(DeliveryRunStatus.ReadyForHandover)
            .HasConversion<int>();
        builder.Property(run => run.PreparedByUserId).IsRequired(false);
        builder.Property(run => run.PreparedOnUtc).IsRequired(false);
        builder.Property(run => run.HandedOverByUserId).IsRequired(false);
        builder.Property(run => run.HandedOverOnUtc).IsRequired(false);
        builder.Property(run => run.DriverCachedOnUtc).IsRequired(false);
        builder.Property(run => run.DriverCacheDeviceId).HasMaxLength(200).IsRequired(false);
        builder.Property(run => run.PaperManifestIssued).IsRequired().HasDefaultValue(false);
        builder.Property(run => run.PaperManifestIssuedOnUtc).IsRequired(false);
        builder.Property(run => run.CashHandoverConfirmedByUserId).IsRequired(false);
        builder.Property(run => run.CashHandoverConfirmedByUsername).HasMaxLength(200).IsRequired(false);
        builder.Property(run => run.CashHandoverConfirmedByFullName).HasMaxLength(200).IsRequired(false);
        builder.Property(run => run.CashHandoverConfirmedOnUtc).IsRequired(false);
        builder.Property(run => run.CashHandoverAmount).HasPrecision(18, 2).IsRequired(false);
        builder.Property(run => run.CashHandoverNote).HasMaxLength(2000).IsRequired(false);
        builder.Property(run => run.Note).HasMaxLength(2000).IsRequired(false);
        builder.Property(run => run.CreatedOnUtc).IsRequired();
        builder.Property(run => run.UpdatedOnUtc).IsRequired(false);

        builder.HasIndex(run => new { run.AssignedDeliveryUserId, run.Status });
        builder.HasIndex(run => run.CreatedOnUtc);

        builder.HasMany(run => run.Items)
            .WithOne()
            .HasForeignKey(item => item.DeliveryRunId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(run => run.Items).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}

public sealed class DeliveryRunItemMap : IEntityTypeConfiguration<DeliveryRunItem>
{
    public void Configure(EntityTypeBuilder<DeliveryRunItem> builder)
    {
        builder.ToTable(nameof(DeliveryRunItem), DbScheme);
        builder.HasKey(item => item.Id);

        builder.Property(item => item.DeliveryRunId).IsRequired();
        builder.Property(item => item.DeliveryNoteId).IsRequired();
        builder.Property(item => item.DeliveryNoteCode).IsRequired().HasMaxLength(100);
        builder.Property(item => item.OrderCode).HasMaxLength(100).IsRequired(false);
        builder.Property(item => item.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(item => item.ShippingPhoneNumber).HasMaxLength(50).IsRequired(false);
        builder.Property(item => item.ShippingAddress).IsRequired().HasMaxLength(1000);
        builder.Property(item => item.AmountToCollect).HasPrecision(18, 2).IsRequired();

        builder.HasIndex(item => item.DeliveryRunId);
        builder.HasIndex(item => item.DeliveryNoteId);
    }
}

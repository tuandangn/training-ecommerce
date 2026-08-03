using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class OrderFulfillmentScheduleMapping : IEntityTypeConfiguration<OrderFulfillmentSchedule>
{
    public void Configure(EntityTypeBuilder<OrderFulfillmentSchedule> builder)
    {
        builder.ToTable(nameof(OrderFulfillmentSchedule), DbScheme);
        builder.HasKey(schedule => schedule.Id);

        builder.Property(schedule => schedule.OrderId).IsRequired();
        builder.Property(schedule => schedule.OrderCode).IsRequired().HasMaxLength(50);
        builder.Property(schedule => schedule.ScheduledFromUtc).IsRequired(false);
        builder.Property(schedule => schedule.ScheduledToUtc).IsRequired(false);
        builder.Property(schedule => schedule.Mode).IsRequired().HasConversion<int>().HasDefaultValue(OrderFulfillmentScheduleMode.AsSoonAsPossible);
        builder.Property(schedule => schedule.Note).HasMaxLength(1000).IsRequired(false);
        builder.Property(schedule => schedule.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(schedule => schedule.CreatedByUserId).IsRequired(false);
        builder.Property(schedule => schedule.CreatedOnUtc).IsRequired();
        builder.Property(schedule => schedule.UpdatedOnUtc).IsRequired(false);
        builder.Property(schedule => schedule.InactivatedOnUtc).IsRequired(false);
        builder.Property(schedule => schedule.InactivatedByUserId).IsRequired(false);

        builder.HasIndex(schedule => schedule.OrderId);
        builder.HasIndex(schedule => schedule.ScheduledFromUtc);
        builder.HasIndex(schedule => schedule.Mode);
        builder.HasIndex(schedule => schedule.IsActive);

        builder.HasOne<Order>().WithMany().HasForeignKey(schedule => schedule.OrderId);
        builder.HasMany(schedule => schedule.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderFulfillmentScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(schedule => schedule.Items).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}

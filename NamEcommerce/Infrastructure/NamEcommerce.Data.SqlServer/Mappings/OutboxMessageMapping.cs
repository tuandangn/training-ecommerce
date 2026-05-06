using NamEcommerce.Domain.Entities.Outbox;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class OutboxMessageMapping : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(nameof(OutboxMessage), DbScheme);
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).IsRequired().HasMaxLength(500);
        builder.Property(m => m.Payload).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(m => m.OccurredOnUtc).IsRequired();
        builder.Property(m => m.ProcessedOnUtc);
        builder.Property(m => m.Error).HasMaxLength(4000);
        builder.Property(m => m.RetryCount).IsRequired();

        // Index để OutboxProcessor query nhanh các message chưa processed theo thứ tự xảy ra.
        builder.HasIndex(m => new { m.ProcessedOnUtc, m.OccurredOnUtc })
            .HasDatabaseName("IX_OutboxMessage_Pending");
    }
}

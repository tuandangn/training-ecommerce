using NamEcommerce.Domain.Entities.Debts;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CassoReconciliationRunMapping : IEntityTypeConfiguration<CassoReconciliationRun>
{
    public void Configure(EntityTypeBuilder<CassoReconciliationRun> builder)
    {
        builder.ToTable("CassoReconciliationRuns", DbScheme);
        builder.HasKey(run => run.Id);

        builder.Property(run => run.StartedAtUtc).IsRequired();
        builder.Property(run => run.FinishedAtUtc).IsRequired(false);
        builder.Property(run => run.FromDate).IsRequired();
        builder.Property(run => run.ToDate).IsRequired();
        builder.Property(run => run.Trigger).HasConversion<int>().IsRequired();
        builder.Property(run => run.TotalRecords).IsRequired();
        builder.Property(run => run.Processed).IsRequired();
        builder.Property(run => run.Matched).IsRequired();
        builder.Property(run => run.Duplicate).IsRequired();
        builder.Property(run => run.Rejected).IsRequired();
        builder.Property(run => run.Ignored).IsRequired();
        builder.Property(run => run.Failed).IsRequired();
        builder.Property(run => run.ErrorMessage).HasMaxLength(500).IsRequired(false);

        builder.HasIndex(run => run.StartedAtUtc);
        builder.HasIndex(run => new { run.FromDate, run.ToDate });
    }
}

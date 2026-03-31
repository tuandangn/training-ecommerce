namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CategoryMapping : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable(nameof(Category), DbScheme);
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.NormalizedName).HasMaxLength(400); 
        builder.Property(c => c.DisplayOrder).IsRequired();
        builder.Property(c => c.CreatedOnUtc).IsRequired();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

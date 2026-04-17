namespace NamEcommerce.Domain.Shared;

public record AppAggregateEntity : AppEntity, ISoftDeletable
{
    public AppAggregateEntity(Guid id) : base(id)
    {
    }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedOnUtc { get; private set; }

    public void Delete()
    {
        IsDeleted = true;
        DeletedOnUtc = DateTime.UtcNow;
    }
}

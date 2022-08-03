namespace NamEcommerce.Domain.Shared;

public record AppAggregateEntity : AppEntity
{
    public AppAggregateEntity(Guid id) : base(id)
    {
    }
}

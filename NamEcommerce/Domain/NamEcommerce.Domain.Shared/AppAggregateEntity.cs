namespace NamEcommerce.Domain.Shared;

public record AppAggregateEntity : AppEntity
{
    public AppAggregateEntity(int id) : base(id)
    {
    }
}

namespace NamEcommerce.Domain.Shared;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedOnUtc { get; }
    void Delete();
}

namespace NamEcommerce.Domain.Shared.Common;

[Serializable]
public record struct SecondaryItemId(Guid PrimaryId, Guid SecondaryId)
{
    public bool IsValid() => PrimaryId != Guid.Empty && SecondaryId != Guid.Empty;

    public static implicit operator SecondaryItemId((Guid primary, Guid secondary) pair) 
        => new(pair.primary, pair.secondary);
}

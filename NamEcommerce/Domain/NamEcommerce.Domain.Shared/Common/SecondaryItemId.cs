namespace NamEcommerce.Domain.Shared.Common;

[Serializable]
public record struct SecondaryItemId(Guid PrimaryId, Guid SecondaryId) : IEquatable<Guid>, IEquatable<SecondaryItemId>
{
    public bool Equals(Guid other) => SecondaryId == other;

    public bool IsValid() => PrimaryId != Guid.Empty && SecondaryId != Guid.Empty;

    public static implicit operator SecondaryItemId((Guid primary, Guid secondary) pair)
        => new(pair.primary, pair.secondary);

    public static bool operator ==(SecondaryItemId itemId, Guid secondaryId) => itemId.SecondaryId == secondaryId;
    public static bool operator ==(Guid secondaryId, SecondaryItemId itemId) => itemId.SecondaryId == secondaryId;
    public static bool operator !=(SecondaryItemId itemId, Guid secondaryId) => itemId.SecondaryId != secondaryId;
    public static bool operator !=(Guid secondaryId, SecondaryItemId itemId) => itemId.SecondaryId != secondaryId;
}

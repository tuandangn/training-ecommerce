using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Metadata;

[Serializable]
public class NormalizableString : IEquatable<NormalizableString>, IComparable<NormalizableString>
{
    private NormalizableString()
    {
        Value = string.Empty;
        NormalizedValue = string.Empty;
    }

    public NormalizableString(string name)
    {
        Value = name ?? string.Empty;
        NormalizedValue = NormalizeText(Value);
    }

    public string Value { get; private set; }
    internal string NormalizedValue { get; private set; }

    public static string NormalizeText(string text)
        => TextHelper.Normalize(text);

    public static implicit operator NormalizableString(string value) => new NormalizableString(value);

    public static implicit operator string(NormalizableString searchableName) => searchableName?.Value ?? string.Empty;

    public override string ToString() => Value;

    public bool Equals(NormalizableString? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return NormalizedValue == other.NormalizedValue;
    }

    public override bool Equals(object? obj) => Equals(obj as NormalizableString);

    public override int GetHashCode() => NormalizedValue.GetHashCode();

    public int CompareTo(NormalizableString? other)
    {
        if (other is null) return 1;
        return string.Compare(this.NormalizedValue, other.NormalizedValue, StringComparison.Ordinal);
    }

    public static bool operator ==(NormalizableString left, NormalizableString right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(NormalizableString left, NormalizableString right) => !(left == right);
    public static bool operator <(NormalizableString left, NormalizableString right) => left is null ? right is not null : left.CompareTo(right) < 0;
    public static bool operator <=(NormalizableString left, NormalizableString right) => left is null || left.CompareTo(right) <= 0;
    public static bool operator >(NormalizableString left, NormalizableString right) => left is not null && left.CompareTo(right) > 0;
    public static bool operator >=(NormalizableString left, NormalizableString right) => left is null ? right is null : left.CompareTo(right) >= 0;
}

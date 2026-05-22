using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Metadata;

[Serializable]
public readonly record struct NormalizableString : IComparable<NormalizableString>
{
    public NormalizableString()
    {
        Value = string.Empty;
        NormalizedValue = string.Empty;
    }

    public NormalizableString(string name)
    {
        Value = name ?? string.Empty;
        NormalizedValue = NormalizeText(Value);
    }

    public string Value { get; init; } = string.Empty;
    internal string NormalizedValue { get; init; } = string.Empty;

    public override string ToString() => Value;

    public int CompareTo(NormalizableString other) => string.Compare(this.NormalizedValue, other.NormalizedValue, StringComparison.Ordinal);

    public static string NormalizeText(string text)
        => TextHelper.Normalize(text);

    public static implicit operator NormalizableString(string? value) => new(value ?? string.Empty);
    public static implicit operator string(NormalizableString searchableName) => searchableName.Value ?? string.Empty;
    public static bool operator <(NormalizableString left, NormalizableString right) => left.CompareTo(right) < 0;
    public static bool operator <=(NormalizableString left, NormalizableString right) => left.CompareTo(right) <= 0;
    public static bool operator >(NormalizableString left, NormalizableString right) => left.CompareTo(right) > 0;
    public static bool operator >=(NormalizableString left, NormalizableString right) => left.CompareTo(right) >= 0;
}

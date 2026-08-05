using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Specifications.Filters;

[Serializable]
public sealed record KeywordFilter
{
    public KeywordFilter(string keywords)
    {
        keywords = keywords.Trim();
        Keywords = keywords;
        UppercaseKeywords = keywords.ToUpper();
        NormalizedKeywords = TextHelper.Normalize(keywords);
    }

    public string Keywords { get; }
    public string UppercaseKeywords { get; }
    public string NormalizedKeywords { get; }

    public static implicit operator KeywordFilter(string value) => new(value);
}

using Microsoft.AspNetCore.Html;
using NamEcommerce.Domain.Shared.Helpers;
using System.Text.Encodings.Web;

namespace NamEcommerce.Web.Services;

public static class HighlighHelper
{
    public static HtmlString Highlight(string content, string? query)
    {
        if (string.IsNullOrEmpty(content))
            return HtmlString.Empty;

        var builder = new HtmlContentBuilder();

        if (string.IsNullOrEmpty(query))
        {
            builder.Append(content);
            return toHtmlString(builder);
        }

        var normalizedQuery = TextHelper.Normalize(query);
        var normalizedContent = TextHelper.Normalize(content);
        var queryLength = normalizedQuery.Length;

        var startIndex = 0;
        var foundIndex = normalizedContent.IndexOf(normalizedQuery, startIndex);

        builder.AppendHtml("<span>");
        while (true)
        {
            if (foundIndex == -1)
            {
                builder.AppendHtml(content[startIndex..]);
                break;
            }
            var normalText = content[startIndex..foundIndex];
            builder.AppendHtml(normalText);

            var highlightText = content.Substring(foundIndex, queryLength);
            builder.AppendHtml($"<strong>{highlightText}</strong>");

            if (foundIndex + queryLength >= content.Length)
                break;

            startIndex = foundIndex + queryLength;
            foundIndex = normalizedContent.IndexOf(normalizedQuery, foundIndex + queryLength);
            if (foundIndex == -1)
            {
                normalText = content[startIndex..];
                builder.AppendHtml(normalText);
                break;
            }
        }
        builder.AppendHtml("</span>");

        return toHtmlString(builder);

        static HtmlString toHtmlString(HtmlContentBuilder builder)
        {
            using var writer = new StringWriter();
            builder.WriteTo(writer, HtmlEncoder.Default);

            return new HtmlString(writer.ToString());
        }
    }
}

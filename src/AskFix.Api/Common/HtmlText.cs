using System.Text.RegularExpressions;

namespace AskFix.Api.Common;

/// <summary>HTML helpers: plain-text extraction for search indexing and excerpts,
/// plus defense-in-depth sanitization (client already sanitizes with DOMPurify).</summary>
public static partial class HtmlText
{
    [GeneratedRegex(@"<br\s*/?>|</p>|</div>|</li>|</pre>|</h[1-6]>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockBreakRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"<script\b[^>]*>.*?</script>|<style\b[^>]*>.*?</style>|<iframe\b[^>]*>.*?</iframe>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DangerousBlockRegex();

    [GeneratedRegex(@"\son\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase)]
    private static partial Regex EventAttrRegex();

    [GeneratedRegex(@"\s(href|src)\s*=\s*([""'])\s*javascript:[^""']*\2", RegexOptions.IgnoreCase)]
    private static partial Regex JsUrlAttrRegex();

    /// <summary>Strip dangerous blocks, event handlers and javascript: URLs from user HTML.</summary>
    public static string Sanitize(string html) =>
        JsUrlAttrRegex().Replace(
            EventAttrRegex().Replace(DangerousBlockRegex().Replace(html, ""), ""),
        "$1=#").Trim();

    /// <summary>Convert user HTML to readable plain text (for FTS index + feed excerpts).</summary>
    public static string ToText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var withBreaks = BlockBreakRegex().Replace(html, "\n");
        var text = TagRegex().Replace(withBreaks, " ");
        return System.Net.WebUtility.HtmlDecode(text)
            .Replace("\u00a0", " ")
            .Trim();
    }

    /// <summary>Plain-text excerpt of specified max length, cutting at a word boundary.</summary>
    public static string Excerpt(string html, int maxLength)
    {
        var text = ToText(html);
        text = Regex.Replace(text, @"\s+", " ").Trim();
        if (text.Length <= maxLength) return text;
        var cut = text[..maxLength];
        var lastSpace = cut.LastIndexOf(' ');
        return (lastSpace > maxLength / 2 ? cut[..lastSpace] : cut).TrimEnd() + "…";
    }
}

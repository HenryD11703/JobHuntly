using System.Text.RegularExpressions;

namespace JobHuntly.Application.Ai;

/// <summary>
/// Pasted text is data, never instructions. We wrap it in tags the prompt declares as untrusted and
/// neutralise any attempt to close or reopen those tags from inside the text. This lowers the risk
/// of prompt injection; it cannot remove it, which is why every output is also validated and
/// nothing is saved without the user's confirmation.
/// </summary>
public static class PromptSafety
{
    public static string Wrap(string tag, string untrustedText)
    {
        var cleaned = TagLike(tag).Replace(untrustedText, m => m.Value.Replace("<", "&lt;").Replace(">", "&gt;"));
        return $"<{tag}>\n{cleaned.Trim()}\n</{tag}>";
    }

    private static Regex TagLike(string tag) =>
        new($@"<\s*/?\s*{Regex.Escape(tag)}\s*/?\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Trims runaway input so one request cannot burn the allowance on a huge prompt.</summary>
    public static string Clip(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..maxChars];
}

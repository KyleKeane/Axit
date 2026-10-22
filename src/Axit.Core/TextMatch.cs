using System.Globalization;
using System.Text;

namespace Axit.Core;

/// <summary>
/// Whether a name holds every word of a typed text, and how well (docs/axit/SPEC.md AX-10.3; from AxStart.Core's
/// <c>ProgramMatch</c> on its second use, the command palette, 2026-10-22). Case and accents are ignored. The rank is
/// by the text's first word: 0 when the name starts with it, 1 when one of its words does, 2 otherwise, so that
/// "win power" sits with the names starting with "win".
/// </summary>
public static class TextMatch
{
    /// <summary>The typed text's words, folded: "Win  Power " is ["win", "power"].</summary>
    public static IReadOnlyList<string> Words(string text) => Fold(text.Trim()).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>The rank of a folded name against the folded words, or null when the name lacks one of them.</summary>
    public static int? Rank(string folded, IReadOnlyList<string> words)
    {
        if (!words.All(word => folded.Contains(word, StringComparison.Ordinal)))
        {
            return null;
        }

        return Rank(folded, words.Count > 0 ? words[0] : string.Empty);
    }

    /// <summary>Lower case without accents, so that "cafe" finds "Café" and "Cafe" finds "café".</summary>
    public static string Fold(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }

    /// <summary>0 when the name starts with the word, 1 when one of its words does, 2 otherwise.</summary>
    private static int Rank(string name, string query)
    {
        if (query.Length == 0 || name.StartsWith(query, StringComparison.Ordinal))
        {
            return 0;
        }

        for (var i = name.IndexOf(query, StringComparison.Ordinal); i > 0; i = name.IndexOf(query, i + 1, StringComparison.Ordinal))
        {
            if (!char.IsLetterOrDigit(name[i - 1]))
            {
                return 1;
            }
        }

        return 2;
    }
}

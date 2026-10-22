using System.Globalization;
using System.Text;
using Axit.Core;

namespace AxStart.Core;

/// <summary>
/// Which programs match the typed text, and in what order (SPEC.md AS-2.2). A name matches when it holds every word of
/// the text, case and accents ignored. A name that starts with the text's first word comes first, then one with a
/// word that starts with it, then any other match; within each, the programs AxStart started more often first, then the names
/// in File Explorer's order. An empty text matches every program, the most started first.
/// </summary>
public static class ProgramMatch
{
    public static IReadOnlyList<ProgramEntry> Find(IReadOnlyList<ProgramEntry> programs, string text, IReadOnlyDictionary<string, int> starts)
    {
        var query = Fold(text.Trim());
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matches = new List<(ProgramEntry Entry, int Rank)>();
        foreach (var entry in programs)
        {
            var name = Fold(entry.Name);
            if (words.All(word => name.Contains(word, StringComparison.Ordinal)))
            {
                // The first word places it: "win power" puts "Windows PowerShell" with the names starting with "win".
                matches.Add((entry, Rank(name, words.Length > 0 ? words[0] : string.Empty)));
            }
        }

        return matches
            .OrderBy(match => match.Rank)
            .ThenByDescending(match => starts.TryGetValue(match.Entry.Name, out var count) ? count : 0)
            .ThenBy(match => match.Entry.Name, NaturalOrder.Instance)
            .Select(match => match.Entry)
            .ToList();
    }

    /// <summary>The same program in the user's and the machine's Start menu is one entry (AS-2.1): the first of each name, ignoring case.</summary>
    public static IReadOnlyList<ProgramEntry> Distinct(IEnumerable<ProgramEntry> programs) =>
        programs.Where(entry => entry.Name.Trim().Length > 0).DistinctBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>What one match says (AS-3.2, AS-3.3): "Calculator, 1 of 2".</summary>
    public static string Spoken(ProgramEntry entry, int index, int count) => $"{entry.Name}, {index + 1} of {count}";

    /// <summary>What a text no program matches says: "No programs match xyz".</summary>
    public static string NoMatch(string text) => $"No programs match {text.Trim()}";

    /// <summary>0 when the name starts with the text, 1 when one of its words does, 2 otherwise.</summary>
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

    /// <summary>Lower case without accents, so that "cafe" finds "Café" and "Cafe" finds "café".</summary>
    private static string Fold(string text)
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
}

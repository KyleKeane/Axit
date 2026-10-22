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
        var words = TextMatch.Words(text);
        var matches = new List<(ProgramEntry Entry, int Rank)>();
        foreach (var entry in programs)
        {
            // The first word places it: "win power" puts "Windows PowerShell" with the names starting with "win".
            if (TextMatch.Rank(TextMatch.Fold(entry.Name), words) is { } rank)
            {
                matches.Add((entry, rank));
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
}

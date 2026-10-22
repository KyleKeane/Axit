namespace Axit.Core;

/// <summary>
/// One line of the command palette (docs/axit/SPEC.md AX-10.2): what the list shows and NVDA says, "Save as, File,
/// Ctrl+Shift+S", and what Enter does with it.
/// </summary>
public sealed record PaletteEntry(string Text, Action Run);

/// <summary>
/// The command palette's text and search (AX-10.2, AX-10.3): an entry's line from its menu item, the matches of the
/// typed text in order, and what is said of them. The window side, which reads the menus and shows the notice, is
/// <c>Axit.Forms.CommandPalette</c> and the window base.
/// </summary>
public static class Palette
{
    /// <summary>A menu item's text as a name: "Save &amp;as..." is "Save as"; a doubled ampersand stays one.</summary>
    public static string Name(string itemText) =>
        itemText.Replace("&&", "\0").Replace("&", string.Empty).Replace("\0", "&").TrimEnd('.', '…').Trim();

    /// <summary>
    /// An entry's line: the name, the menus it is in, the key when it has one, "checked" when it is:
    /// "Word wrap, Edit, Ctrl+Shift+W, checked".
    /// </summary>
    public static string Text(string name, IEnumerable<string> path, string? key, bool isChecked)
    {
        var parts = new List<string> { name };
        parts.AddRange(path);
        if (!string.IsNullOrEmpty(key))
        {
            parts.Add(key);
        }

        if (isChecked)
        {
            parts.Add("checked");
        }

        return string.Join(", ", parts);
    }

    /// <summary>
    /// The entries holding every word of the text, the ones whose line starts with its first word first, then those
    /// with a word starting with it, then the rest; within each, in the order given, which is the menus' (AX-10.3).
    /// An empty text gives every entry.
    /// </summary>
    public static IReadOnlyList<PaletteEntry> Find(IReadOnlyList<PaletteEntry> entries, string text)
    {
        var words = TextMatch.Words(text);
        var matches = new List<(PaletteEntry Entry, int Rank)>();
        foreach (var entry in entries)
        {
            if (TextMatch.Rank(TextMatch.Fold(entry.Text), words) is { } rank)
            {
                matches.Add((entry, rank));
            }
        }

        // OrderBy is stable, so the menus' order stays within a rank.
        return matches.OrderBy(match => match.Rank).Select(match => match.Entry).ToList();
    }

    /// <summary>What one match says (AX-10.4): "Save as, File, Ctrl+Shift+S, 1 of 3".</summary>
    public static string Spoken(PaletteEntry entry, int index, int count) => $"{entry.Text}, {index + 1} of {count}";

    /// <summary>What a text nothing matches says: "No commands match xyz".</summary>
    public static string NoMatch(string text) => $"No commands match {text.Trim()}";
}

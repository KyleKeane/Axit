using System.Text;
using Axit.Core;

namespace AxClip.Core;

/// <summary>One item of the history (SPEC.md CP-2.5, CP-3): the copied text and when it was copied, saved as it is (CP-6.1).</summary>
public sealed record ClipEntry(string Text, DateTime Copied)
{
    public const int LabelLength = 120;

    /// <summary>"git status --porcelain", or "First line…, 3 lines": the first line with text, its spaces run together, cut at 120 characters.</summary>
    public string Label
    {
        get
        {
            var lines = Text.Replace("\r\n", "\n").Split('\n');
            var first = lines.FirstOrDefault(line => line.Trim().Length > 0) ?? string.Empty;
            var squeezed = new StringBuilder();
            foreach (var c in first.Trim())
            {
                if (!char.IsWhiteSpace(c))
                {
                    squeezed.Append(c);
                }
                else if (squeezed.Length > 0 && squeezed[^1] != ' ')
                {
                    squeezed.Append(' ');
                }
            }

            var label = squeezed.Length > LabelLength ? squeezed.ToString(0, LabelLength) + "…" : squeezed.ToString();
            return LineCount > 1 ? $"{label}, {LineCount} lines" : label;
        }
    }

    /// <summary>The number of lines from the first to the last, the blank ends left out, for the label and the notice's title (CP-4.3).</summary>
    public int LineCount => Text.Trim('\r', '\n').Replace("\r\n", "\n").Split('\n').Length;
}

/// <summary>
/// The history (SPEC.md CP-3.2): the items newest first, a text once (a copy again moves it to the top), at most
/// <see cref="MaxItems"/>; the search of CP-2.3 over the labels' texts, newest first.
/// </summary>
public sealed class ClipHistory
{
    public const int MaxItems = 200;

    private readonly List<ClipEntry> _items = [];

    public ClipHistory(IEnumerable<ClipEntry>? items = null)
    {
        foreach (var item in (items ?? []).Take(MaxItems))
        {
            if (!_items.Any(kept => kept.Text == item.Text))
            {
                _items.Add(item);
            }
        }
    }

    /// <summary>The items, newest first.</summary>
    public IReadOnlyList<ClipEntry> Items => _items;

    /// <summary>A copy's text as the newest item; one already kept moves to the top with the new time.</summary>
    public void Add(string text, DateTime copied)
    {
        _items.RemoveAll(item => item.Text == text);
        _items.Insert(0, new ClipEntry(text, copied));
        if (_items.Count > MaxItems)
        {
            _items.RemoveRange(MaxItems, _items.Count - MaxItems);
        }
    }

    public void Remove(ClipEntry entry) => _items.Remove(entry);

    public void Clear() => _items.Clear();

    /// <summary>The items whose text holds every word typed, case and accents ignored, newest first; all of them for an empty text.</summary>
    public IReadOnlyList<ClipEntry> Find(string typed)
    {
        var words = TextMatch.Words(typed);
        return words.Count == 0 ? _items : _items.Where(item => TextMatch.Rank(TextMatch.Fold(item.Text), words) is not null).ToList();
    }

    /// <summary>"git status, 2 of 12" (CP-2.4).</summary>
    public static string Spoken(ClipEntry entry, int index, int count) => $"{entry.Label}, {index + 1} of {count}";

    /// <summary>What the list says with nothing to show (CP-2.3, CP-4.5).</summary>
    public string Nothing(string typed) =>
        _items.Count == 0 || typed.Trim().Length == 0 ? "The clipboard history is empty" : $"Nothing in the clipboard history matches {typed.Trim()}";
}

/// <summary>
/// Whether a copy is kept (SPEC.md CP-3.1, CP-D2): its text, and the formats the copying program put beside it, the
/// ones a password manager sets to keep a copy out of clipboard histories.
/// </summary>
public static class ClipPolicy
{
    public const int MaxLength = 1_000_000;

    /// <summary>The formats that alone keep a copy out.</summary>
    public static readonly IReadOnlyList<string> ExcludingFormats = ["ExcludeClipboardContentFromMonitorProcessing", "Clipboard Viewer Ignore"];

    /// <summary>The format whose value 0 keeps a copy out of clipboard history (Windows' own history honours it too).</summary>
    public const string HistoryFormat = "CanIncludeInClipboardHistory";

    /// <param name="excluded">Whether the copy carries one of <see cref="ExcludingFormats"/>.</param>
    /// <param name="historyValue">The value of <see cref="HistoryFormat"/> when the copy carries it, null otherwise.</param>
    public static bool Keeps(string? text, bool excluded, int? historyValue, bool paused) =>
        !paused && !excluded && historyValue != 0 && text is { Length: > 0 and <= MaxLength } && text.Trim().Length > 0;
}

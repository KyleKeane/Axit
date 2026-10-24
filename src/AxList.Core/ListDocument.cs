namespace AxList.Core;

/// <summary>
/// A list file's lines (SPEC.md §5) and the changes of LI-4, each a pure operation that returns the new document and
/// what to say, so that the window only reads, shows, writes and announces. A line nothing changed is written back
/// exactly as it was read; the changed lines, and the numbered items numbered again, are rendered in axList's shape.
/// Every index is a line of the file, blank lines included.
/// </summary>
public sealed class ListDocument
{
    private readonly bool _finalNewline;

    private ListDocument(IReadOnlyList<ListLine> lines, bool finalNewline)
    {
        Lines = lines;
        _finalNewline = finalNewline;
    }

    public IReadOnlyList<ListLine> Lines { get; }

    /// <summary>The document of a file's text, its line ends "\r\n" as <c>TextDocument</c> gives them.</summary>
    public static ListDocument Parse(string text)
    {
        if (text.Length == 0)
        {
            return new ListDocument([], finalNewline: true);
        }

        var parts = text.Split("\r\n");
        var final = parts[^1].Length == 0;
        var lines = (final ? parts[..^1] : parts).Select(ListLine.Parse).ToList();
        return new ListDocument(lines, final);
    }

    /// <summary>The text to write, "\r\n" between lines (<c>TextDocument.ToBytes</c> gives the file its own line end).</summary>
    public string ToText() => Lines.Count == 0 ? string.Empty : string.Join("\r\n", Lines.Select(line => line.Raw)) + (_finalNewline ? "\r\n" : string.Empty);

    public int TaskCount => Lines.Count(line => line.IsTask);

    public int OpenTaskCount => Lines.Count(line => line.IsTask && !line.Done);

    /// <summary>The lines the Items view shows (LI-4.1): every line but the blank ones, and with <paramref name="hideDone"/> not the done tasks.</summary>
    public IReadOnlyList<int> Shown(bool hideDone)
    {
        var shown = new List<int>();
        for (var i = 0; i < Lines.Count; i++)
        {
            if (Lines[i].Kind != LineKind.Blank && !(hideDone && Lines[i].IsTask && Lines[i].Done))
            {
                shown.Add(i);
            }
        }

        return shown;
    }

    /// <summary>
    /// The level of every line, 1 for an item at the top: an item is one deeper than the nearest item before it with a
    /// smaller width; a heading starts over. Lines that are not items take the level of the item they follow, or 1.
    /// </summary>
    public IReadOnlyList<int> Levels()
    {
        var levels = new int[Lines.Count];
        var widths = new Stack<int>();
        for (var i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];
            if (line.Kind == LineKind.Heading)
            {
                widths.Clear();
            }

            if (line.Kind != LineKind.Item)
            {
                levels[i] = 1;
                continue;
            }

            while (widths.Count > 0 && widths.Peek() >= line.Width)
            {
                widths.Pop();
            }

            levels[i] = widths.Count + 1;
            widths.Push(line.Width);
        }

        return levels;
    }

    /// <summary>The Items view's text of a line (LI-4.1).</summary>
    public string Label(int index)
    {
        var line = Lines[index];
        var text = line.Text.Length == 0 ? "(empty)" : line.Text;
        var label = line.Kind switch
        {
            LineKind.Heading => "Heading: " + text,
            LineKind.Item => (line.IsNumbered ? $"{line.Number}. {text}" : text) + (line.IsTask ? (line.Done ? ", task, done" : ", task, not done") : string.Empty),
            _ => text,
        };
        var level = Levels()[index];
        return line.Kind == LineKind.Item && level > 1 ? $"{label}, level {level}" : label;
    }

    /// <summary>The name of an item for an announcement: its text, or "(empty)".</summary>
    public string NameOf(int index) => Lines[index].Text.Length == 0 ? "(empty)" : Lines[index].Text;

    // ---- the changes (LI-4) ----

    /// <summary>Space (LI-4.2): a task checked or unchecked; null when the line is not a task.</summary>
    public (ListDocument Document, string Said)? Toggle(int index)
    {
        var line = Lines[index];
        if (!line.IsTask)
        {
            return null;
        }

        return (Replace(index, line.With(done: !line.Done)), (line.Done ? "Not done: " : "Done: ") + NameOf(index));
    }

    /// <summary>Enter (LI-4.3): the line's text changed, its marker and state kept.</summary>
    public ListDocument SetText(int index, string text) => Replace(index, Lines[index].With(text: text.Trim()));

    /// <summary>Ctrl+T (LI-4.7): a bullet or numbered item made a task, a task made a bullet; null for a line that is not an item.</summary>
    public (ListDocument Document, string Said)? ToggleTask(int index)
    {
        var line = Lines[index];
        if (line.Kind != LineKind.Item)
        {
            return null;
        }

        var task = !line.IsTask;
        return (Replace(index, line.With(isTask: task, done: false)), NameOf(index) + (task ? " is a task" : " is not a task"));
    }

    /// <summary>
    /// Ctrl+N (LI-4.4): a new item after the line at <paramref name="after"/> and its nested lines, at its level and of
    /// its kind when it is an item; after a heading or a text line, or in a list without lines (null), a task like the
    /// first item of the list, or "- [ ] ". The new line's index comes back.
    /// </summary>
    public (ListDocument Document, int Index) Insert(int? after, string text)
    {
        text = text.Trim();
        var lines = Lines.ToList();
        ListLine line;
        int at;
        if (after is { } index && Lines[index].Kind == LineKind.Item)
        {
            line = Lines[index].Sibling(text, Lines[index].Width);
            at = SubtreeEnd(index);
        }
        else
        {
            var model = Lines.FirstOrDefault(l => l.Kind == LineKind.Item && l.Width == 0);
            line = model is null ? ListLine.NewTask(text, 0) : model.Sibling(text, 0) with { Number = 1 };
            line = line with { Raw = line.Render() };
            at = after is { } heading ? heading + 1 : LastNonBlank() + 1;
        }

        lines.Insert(at, line);
        return (new ListDocument(lines, _finalNewline || Lines.Count == 0).Renumbered(), at);
    }

    /// <summary>Delete (LI-4.5): the line and its nested lines removed; how many nested lines went with it.</summary>
    public (ListDocument Document, int Nested) Remove(int index)
    {
        var end = Lines[index].Kind == LineKind.Item ? SubtreeEnd(index) : index + 1;
        var lines = Lines.ToList();
        lines.RemoveRange(index, end - index);
        return (new ListDocument(lines, _finalNewline).Renumbered(), end - index - 1);
    }

    /// <summary>
    /// Alt+Up and Alt+Down (LI-4.6): the item and its nested lines past the item before or after it at the same width,
    /// with that one's nested lines; the item's new index, or null at the top or the bottom of its level.
    /// </summary>
    public (ListDocument Document, int Index)? Move(int index, int direction)
    {
        if (Lines[index].Kind != LineKind.Item)
        {
            return null;
        }

        var end = SubtreeEnd(index);
        var block = Lines.Skip(index).Take(end - index).ToList();
        if (direction < 0)
        {
            if (PreviousSibling(index) is not { } sibling)
            {
                return null;
            }

            var lines = Lines.ToList();
            lines.RemoveRange(index, end - index);
            lines.InsertRange(sibling, block);
            return (new ListDocument(lines, _finalNewline).Renumbered(), sibling);
        }

        var next = end;
        while (next < Lines.Count && Lines[next].Kind == LineKind.Blank)
        {
            next++;
        }

        if (next >= Lines.Count || Lines[next].Kind != LineKind.Item || Lines[next].Width != Lines[index].Width)
        {
            return null;
        }

        var nextEnd = SubtreeEnd(next);
        var moved = Lines.ToList();
        moved.InsertRange(nextEnd, block);
        moved.RemoveRange(index, end - index);
        return (new ListDocument(moved, _finalNewline).Renumbered(), nextEnd - block.Count);
    }

    /// <summary>
    /// Alt+Right (LI-4.6): the item and its nested lines one level deeper, under the item before it at its width; null
    /// with no such item. Alt+Left: one level out, to its parent's width; null at the top level.
    /// </summary>
    public ListDocument? Indent(int index, int direction)
    {
        var line = Lines[index];
        if (line.Kind != LineKind.Item)
        {
            return null;
        }

        int width;
        if (direction > 0)
        {
            if (PreviousSibling(index) is not { } sibling)
            {
                return null;
            }

            width = Lines[sibling].Width + 2;
        }
        else
        {
            if (line.Width == 0)
            {
                return null;
            }

            width = Parent(index) is { } parent ? Lines[parent].Width : 0;
        }

        var delta = width - line.Width;
        var end = SubtreeEnd(index);
        var lines = Lines.ToList();
        for (var i = index; i < end; i++)
        {
            if (lines[i].Kind != LineKind.Blank)
            {
                lines[i] = lines[i].With(width: Math.Max(0, lines[i].Width + delta));
            }
        }

        return new ListDocument(lines, _finalNewline).Renumbered();
    }

    /// <summary>The line at an index is still the one the window showed: same kind, same text, same state (LI-6.2).</summary>
    public bool Holds(int index, ListLine shown) =>
        index >= 0 && index < Lines.Count && Lines[index] is var line && line.Kind == shown.Kind && line.Text == shown.Text
        && line.IsTask == shown.IsTask && line.Done == shown.Done && line.Width == shown.Width;

    // ---- the structure ----

    /// <summary>The index after the item's nested lines: the lines that follow with a greater width, up to a blank line or a line at its width or less.</summary>
    public int SubtreeEnd(int index)
    {
        var width = Lines[index].Width;
        var end = index + 1;
        while (end < Lines.Count && Lines[end].Kind is LineKind.Item or LineKind.Text && Lines[end].Width > width)
        {
            end++;
        }

        return end;
    }

    /// <summary>The start of the item before this one at the same width under the same parent, past blank lines; null when a parent, a heading or a text line comes first.</summary>
    private int? PreviousSibling(int index)
    {
        var width = Lines[index].Width;
        for (var k = index - 1; k >= 0; k--)
        {
            var line = Lines[k];
            if (line.Kind == LineKind.Blank || (line.Kind is LineKind.Item or LineKind.Text && line.Width > width))
            {
                continue;
            }

            return line.Kind == LineKind.Item && line.Width == width ? k : null;
        }

        return null;
    }

    /// <summary>The nearest item before this one with a smaller width; null at the top level or after a heading.</summary>
    private int? Parent(int index)
    {
        var width = Lines[index].Width;
        for (var k = index - 1; k >= 0; k--)
        {
            if (Lines[k].Kind == LineKind.Heading)
            {
                return null;
            }

            if (Lines[k].Kind == LineKind.Item && Lines[k].Width < width)
            {
                return k;
            }
        }

        return null;
    }

    private int LastNonBlank()
    {
        for (var i = Lines.Count - 1; i >= 0; i--)
        {
            if (Lines[i].Kind != LineKind.Blank)
            {
                return i;
            }
        }

        return -1;
    }

    private ListDocument Replace(int index, ListLine line)
    {
        var lines = Lines.ToList();
        lines[index] = line;
        return new ListDocument(lines, _finalNewline);
    }

    /// <summary>
    /// The numbered items numbered from 1 in every run at one width (LI-4.4): a run goes on past nested lines and blank
    /// lines and ends at a heading, at an item of another kind at its width, or at a line with a smaller width. Only
    /// the lines whose number changes are rendered again.
    /// </summary>
    private ListDocument Renumbered()
    {
        var lines = Lines.ToList();
        var counters = new Dictionary<int, int>();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.Kind == LineKind.Heading)
            {
                counters.Clear();
                continue;
            }

            if (line.Kind is LineKind.Blank)
            {
                continue;
            }

            foreach (var deeper in counters.Keys.Where(width => width > line.Width).ToList())
            {
                counters.Remove(deeper);
            }

            if (line.Kind != LineKind.Item)
            {
                continue;
            }

            if (!line.IsNumbered)
            {
                counters.Remove(line.Width);
                continue;
            }

            var number = counters.TryGetValue(line.Width, out var last) ? last + 1 : 1;
            counters[line.Width] = number;
            if (line.Number != number)
            {
                lines[i] = line.With(number: number);
            }
        }

        return new ListDocument(lines, _finalNewline);
    }
}

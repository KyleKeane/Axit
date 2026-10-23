namespace AxFile.Core;

/// <summary>
/// What one change of the marks did (SPEC.md AF-3.7, AF-9.2), for the window to apply: the rows of the shown entries
/// whose mark flipped, so that those items alone are rewritten; the marks before the change, which its line's undo
/// puts back; the line of the Actions list, null when nothing is recorded (nothing changed, the marks kept through a
/// read, the marks put back); whether a Shift range began with it, a new line, rather than went on, its line
/// rewritten; the entry the line leads to; and what the key says, empty when it says nothing.
/// </summary>
public sealed record MarkChange(IReadOnlyList<int> Changed, IReadOnlyList<string> Before, string? Line, bool RangeBegins, string? Select, string Said);

/// <summary>
/// The marks on a folder's entries (SPEC.md AF-3.7), the selection the operations act on: a set of paths among the
/// entries the list shows, and the Shift range being made, where it began, where it has reached and the marks there
/// were before it. Each key is one method over the shown entries and the current row, returning a
/// <see cref="MarkChange"/> the window applies to its items and the Actions list (<c>FileForm.ApplyMarks</c>). The
/// marks are on shown entries only: what the list leaves out (<see cref="Keep"/>) is not marked, so that the count
/// said and the entries acted on are one set. Pure, tested. (Until 2026-10-23 the marks lived in five places of the
/// window that had to agree.)
/// </summary>
public sealed class Marks
{
    private readonly HashSet<string> _marked = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The marks there were before the Shift range began, which the range keeps and its line's undo puts back.</summary>
    private HashSet<string> _base = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Where the Shift range began and where it has reached, rows of the shown entries; -1 while none is being made.</summary>
    private int _anchor = -1;
    private int _end = -1;

    public int Count => _marked.Count;

    public bool Contains(string path) => _marked.Contains(path);

    /// <summary>The marked paths, a copy.</summary>
    public IReadOnlyList<string> Paths => [.. _marked];

    /// <summary>Whether a Shift range is being made: the next Shift key goes on with it while the current entry is where it reached.</summary>
    public bool RangeOn => _anchor >= 0;

    /// <summary>Space: the current entry marked, or unmarked again; "Selected" or "Deselected", with the count when more than that one is marked.</summary>
    public MarkChange Toggle(IReadOnlyList<Entry> shown, int current)
    {
        var before = Paths;
        var entry = shown[current];
        var on = _marked.Add(entry.Path);
        if (!on)
        {
            _marked.Remove(entry.Path);
        }

        EndRange();
        var word = on ? "Selected" : "Deselected";
        var said = _marked.Count > (on ? 1 : 0) ? $"{word}, {_marked.Count} selected" : word;
        return new([current], before, $"{word} {entry.Name}", false, entry.Name, said);
    }

    /// <summary>Ctrl+A: every entry shown marked, "n selected"; the line "Selected all, n items", none when nothing changed.</summary>
    public MarkChange All(IReadOnlyList<Entry> shown, int current)
    {
        var before = Paths;
        var changed = new List<int>();
        for (var i = 0; i < shown.Count; i++)
        {
            if (_marked.Add(shown[i].Path))
            {
                changed.Add(i);
            }
        }

        EndRange();
        return new(changed, before, changed.Count > 0 ? $"Selected all, {ActionLog.Items(_marked.Count)}" : null, false, NameAt(shown, current), $"{_marked.Count} selected");
    }

    /// <summary>Ctrl+Shift+A, Escape: every mark gone, "None selected"; the line "Deselected all, n items", none when there was none.</summary>
    public MarkChange None(IReadOnlyList<Entry> shown, int current)
    {
        var before = Paths;
        var changed = new List<int>();
        for (var i = 0; i < shown.Count; i++)
        {
            if (_marked.Contains(shown[i].Path))
            {
                changed.Add(i);
            }
        }

        _marked.Clear();
        EndRange();
        return new(changed, before, before.Count > 0 ? $"Deselected all, {ActionLog.Items(before.Count)}" : null, false, NameAt(shown, current), "None selected");
    }

    /// <summary>
    /// Shift+Space: every entry from the nearest marked entry to the current one marked, above or below, the one above
    /// when both are as near, "Selected, n selected"; with no other entry marked it is Space (<see cref="Toggle"/>).
    /// </summary>
    public MarkChange ToHere(IReadOnlyList<Entry> shown, int current)
    {
        var nearest = -1;
        for (var distance = 1; nearest < 0 && (current - distance >= 0 || current + distance < shown.Count); distance++)
        {
            if (current - distance >= 0 && _marked.Contains(shown[current - distance].Path))
            {
                nearest = current - distance;
            }
            else if (current + distance < shown.Count && _marked.Contains(shown[current + distance].Path))
            {
                nearest = current + distance;
            }
        }

        if (nearest < 0)
        {
            return Toggle(shown, current);
        }

        var before = Paths;
        var changed = new List<int>();
        for (var i = Math.Min(nearest, current); i <= Math.Max(nearest, current); i++)
        {
            if (_marked.Add(shown[i].Path))
            {
                changed.Add(i);
            }
        }

        EndRange();
        return new(changed, before, $"Selected {ActionLog.Items(Math.Abs(current - nearest) + 1)}", false, shown[current].Name, $"Selected, {_marked.Count} selected");
    }

    /// <summary>
    /// Shift with Up, Down, Home or End: the current entry moves from <paramref name="current"/> to
    /// <paramref name="target"/>, a row of the list, and every entry between where the range began and there is
    /// marked. The range begins at the current entry when none is being made or the current entry is not where the
    /// last Shift key left it, and keeps the marks made before it; moving back shrinks it. Nothing is said: each
    /// entry passed reads ", selected" by itself. The line is "Selected n items", counting from the range's start to
    /// its end, new when the range begins and rewritten as it goes on; its undo puts back the marks before the range.
    /// </summary>
    public MarkChange Extend(IReadOnlyList<Entry> shown, int current, int target)
    {
        var begins = _anchor < 0 || current != _end;
        if (begins)
        {
            _anchor = current;
            _base = new HashSet<string>(_marked, StringComparer.OrdinalIgnoreCase);
        }

        _end = target;
        var before = new HashSet<string>(_marked, StringComparer.OrdinalIgnoreCase);
        _marked.Clear();
        _marked.UnionWith(_base);
        for (var i = Math.Min(_anchor, target); i <= Math.Max(_anchor, target); i++)
        {
            _marked.Add(shown[i].Path);
        }

        return new(Flipped(shown, before), [.. _base], $"Selected {ActionLog.Items(Math.Abs(target - _anchor) + 1)}", begins, shown[target].Name, string.Empty);
    }

    /// <summary>The list made again (a read, a new order or filter; AF-3.8): the marks on entries it no longer shows are gone, and the range ends. No line, nothing said; the window fills its items anew.</summary>
    public MarkChange Keep(IReadOnlyList<Entry> shown)
    {
        var before = Paths;
        _marked.IntersectWith(shown.Select(entry => entry.Path));
        EndRange();
        return new([], before, null, false, null, string.Empty);
    }

    /// <summary>Ctrl+Z on a selection line (AF-9.3): the marks as they were before it, among the entries shown (entries gone since are skipped), the range ended; "Undone: n selected" or "Undone: none selected".</summary>
    public MarkChange Restore(IReadOnlyList<string> marks, IReadOnlyList<Entry> shown)
    {
        var before = new HashSet<string>(_marked, StringComparer.OrdinalIgnoreCase);
        var wanted = new HashSet<string>(marks, StringComparer.OrdinalIgnoreCase);
        _marked.Clear();
        _marked.UnionWith(shown.Select(entry => entry.Path).Where(wanted.Contains));
        EndRange();
        return new(Flipped(shown, before), [.. before], null, false, null, _marked.Count > 0 ? $"Undone: {_marked.Count} selected" : "Undone: none selected");
    }

    /// <summary>Another folder shown: no marks, no range (AF-3.7).</summary>
    public void Clear()
    {
        _marked.Clear();
        EndRange();
    }

    private void EndRange()
    {
        _anchor = -1;
        _end = -1;
    }

    /// <summary>The rows whose mark differs from <paramref name="before"/>.</summary>
    private List<int> Flipped(IReadOnlyList<Entry> shown, HashSet<string> before)
    {
        var changed = new List<int>();
        for (var i = 0; i < shown.Count; i++)
        {
            if (before.Contains(shown[i].Path) != _marked.Contains(shown[i].Path))
            {
                changed.Add(i);
            }
        }

        return changed;
    }

    private static string? NameAt(IReadOnlyList<Entry> shown, int current) => current >= 0 && current < shown.Count ? shown[current].Name : null;
}

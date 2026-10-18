namespace Axit.Core.Transcript;

/// <summary>
/// The transcript's lines and what a <see cref="Vt.Screen"/> does to them: a line for a row that gets its first text,
/// a committed line when its row scrolls off the top or is let go, a removed line when its row is discarded, and the
/// new text of a rewritten row at the end of a frame. The screen knows nothing beyond this. Each app's model derives
/// from it and adds what its program's output means: AxClaude's SessionModel (Claude Code's labels,
/// chrome and echoes), AxTerminal's model (a shell's prompts and marks). The seam that let the screen, the parser
/// and the mirror move into the shared project on their second use (docs/axterminal/SPEC.md AT-D6).
/// </summary>
public abstract class LineStore
{
    /// <summary>The lines in transcript order. Screen rows' lines sit at the end, so they are searched from the end.</summary>
    protected readonly List<Line> _lines = [];
    private int _nextId;

    public IReadOnlyList<Line> Lines => _lines;

    /// <summary>A line of the app's own (a marker, a system line, a bookmark) with the next id; the caller places it.</summary>
    protected Line NewLine(LineKind kind, string text, int headingLevel = 0) => new(_nextId++, kind, text) { HeadingLevel = headingLevel };

    /// <summary>
    /// A screen row got its first text: a new plain line, placed before the line of the next row below that has one
    /// (so that interior blank rows keep their place), or at the end.
    /// </summary>
    protected internal virtual Line CreateLine(Line? before)
    {
        var line = NewLine(LineKind.Plain, string.Empty);
        var index = before is null ? -1 : _lines.LastIndexOf(before);
        if (index < 0)
        {
            _lines.Add(line);
        }
        else
        {
            _lines.Insert(index, line);
        }

        return line;
    }

    /// <summary>The line's row scrolled off the top of the screen, or the screen let its rows go: the line is final.</summary>
    protected internal virtual void Commit(Line line)
    {
        line.Committed = true;
        line.Row = null;
    }

    /// <summary>The line's row was discarded (a row pushed out of a scroll region by an insert, or a deleted row): the line goes.</summary>
    protected internal virtual void Remove(Line line)
    {
        var index = _lines.LastIndexOf(line);
        if (index >= 0)
        {
            _lines.RemoveAt(index);
        }

        line.Row = null;
    }

    /// <summary>At the end of a frame a row's text differs from its line's (<see cref="Vt.Screen.PullRowText"/>): the line takes it.</summary>
    protected internal virtual void TextChanged(Line line, string text) => line.Text = text;
}

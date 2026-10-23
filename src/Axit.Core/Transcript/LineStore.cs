using System.Text;
using Axit.Core.Vt;

namespace Axit.Core.Transcript;

/// <summary>
/// The transcript of a console (docs/axterminal/SPEC.md AT-D6): the lines in order, the screen the program draws on
/// with the parser that feeds it (<see cref="Feed"/>, <see cref="NewScreen"/>), the bookmarks, the cap on the line
/// count, and what the screen does to the lines: a line for a row that gets its first text, a committed line when
/// its row scrolls off the top or is let go, a removed line when its row is discarded, and the new text of a
/// rewritten row at the end of a frame. The screen knows nothing beyond this. Each app's model derives from it and
/// adds what its program's output means: axClaude's <c>SessionModel</c> (Claude Code's labels, chrome and echoes),
/// axTerminal's <c>TerminalModel</c> (a shell's prompts and marks). It began as the seam that let the screen, the
/// parser and the mirror move into the shared project (2026-10-18); the decoder, the parser, the screen, the
/// bookmarks and the cap, which both models had copied, moved in on 2026-10-23. Single threaded.
/// </summary>
public abstract class LineStore
{
    /// <summary>The lines in transcript order. Screen rows' lines sit at the end, so they are searched from the end.</summary>
    protected readonly List<Line> _lines = [];

    /// <summary>The screen of the current run of the program, made by <see cref="NewScreen"/>; null in a store without one (a test driving a screen of its own).</summary>
    protected Screen _screen = null!;

    private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
    private char[] _chars = new char[4096];
    private VtParser? _parser;
    private int _nextId;
    private int _bookmarkCount;

    /// <param name="columns">The console's size, which the screen takes; 0 for a store without a screen of its own.</param>
    protected LineStore(int columns = 0, int rows = 0)
    {
        Columns = columns;
        Rows = rows;
    }

    public int Columns { get; }
    public int Rows { get; }

    public IReadOnlyList<Line> Lines => _lines;

    /// <summary>Above this many lines the oldest tenth of the transcript is dropped (axClaude FR-3.6, axTerminal AT-4.4).</summary>
    public int MaxLines { get; set; } = 20000;

    /// <summary>Show rows the program hard-wrapped at the console width as one line (axClaude FR-3.2a, axTerminal AT-4.1); each model has the rule.</summary>
    public bool JoinWrappedLines { get; set; } = true;

    /// <summary>The time of sending in the Input and Output markers (axClaude FR-4.6, axTerminal AT-3.2).</summary>
    public bool TimeStamps { get; set; }

    /// <summary>The clock the time stamps use; tests replace it.</summary>
    public Func<DateTime> Clock { get; set; } = () => DateTime.Now;

    /// <summary>Messages or commands sent since the start, or since the model was cleared: the n of the last exchange block.</summary>
    public int ExchangeCount { get; protected set; }

    /// <summary>The visible lines changed: a frame ended, a block or a system line went in, a bookmark came or went.</summary>
    public event Action? Changed;

    /// <summary>The program rang the bell.</summary>
    public event Action? Bell;

    /// <summary>The raw console bytes: decoded as UTF-8 across chunk boundaries and parsed onto the screen. After <see cref="NewScreen"/>.</summary>
    public void Feed(ReadOnlySpan<byte> bytes)
    {
        if (_chars.Length < bytes.Length + 4)
        {
            _chars = new char[Math.Max(bytes.Length + 4, _chars.Length * 2)];
        }

        var count = _decoder.GetChars(bytes, _chars, false);
        _parser!.Feed(_chars.AsSpan(0, count));
    }

    /// <summary>The output went quiet (the window calls this after 100 ms without output, and at exit): the frame's work.</summary>
    public virtual void EndFrame() => CompleteFrame();

    /// <summary>A line of the app's own (<c>System: …</c>), placed where the model keeps the program's live rows out of the way.</summary>
    public abstract void AddSystemLine(string text);

    /// <summary>
    /// Drops a bookmark line in front of the line (or of the first row of a joined group), or takes the bookmark away
    /// when the line is one or has one in front of it (axClaude FR-3.9): an app line, <c>Bookmark 3</c>, read in place,
    /// found with k and saved with the transcript. Null when the line is not in the transcript.
    /// </summary>
    public (Line Bookmark, bool Added)? ToggleBookmark(Line line)
    {
        var first = _lines.LastIndexOf(line);
        if (first < 0)
        {
            return null;
        }

        var before = VisibleBefore(first);
        while (_lines[first].JoinedToPrevious && before >= 0)
        {
            first = before;
            before = VisibleBefore(first);
        }

        var existing = line.Kind == LineKind.Bookmark ? line
            : before >= 0 && _lines[before].Kind == LineKind.Bookmark ? _lines[before]
            : null;
        if (existing is not null)
        {
            _lines.Remove(existing);
            RaiseChanged();
            return (existing, false);
        }

        var bookmark = NewLine(LineKind.Bookmark, $"Bookmark {++_bookmarkCount}");
        _lines.Insert(first, bookmark);
        RaiseChanged();
        return (bookmark, true);
    }

    // ---- what the model derives from ----

    /// <summary>The frame's work at its end (the cursor shown again, or the output quiet): the row text pulled into the lines and the model's own passes; it ends with <see cref="RaiseChanged"/>.</summary>
    protected abstract void CompleteFrame();

    /// <summary>
    /// A fresh screen and parser for the next run of the program in the same window: the model attaches its own
    /// handlers to the screen's events (marks, the cursor homed, the title) and keeps the lines. The screen raises
    /// <see cref="CompleteFrame"/> at each frame end and <see cref="Bell"/> here.
    /// </summary>
    protected Screen NewScreen()
    {
        _screen = new Screen(Columns, Rows, this);
        _screen.FrameEnd += CompleteFrame;
        _screen.Bell += () => Bell?.Invoke();
        _parser = new VtParser(_screen);
        _decoder.Reset();
        return _screen;
    }

    protected void RaiseChanged() => Changed?.Invoke();

    /// <summary>The bookmarks count from 1 again (a cleared transcript).</summary>
    protected void ResetBookmarks() => _bookmarkCount = 0;

    /// <summary>A line of the app's own (a marker, a system line, a bookmark) with the next id; the caller places it.</summary>
    protected Line NewLine(LineKind kind, string text, int headingLevel = 0) => new(_nextId++, kind, text) { HeadingLevel = headingLevel };

    /// <summary>The index of the nearest visible line before <paramref name="index"/>, or -1.</summary>
    protected int VisibleBefore(int index)
    {
        for (var i = index - 1; i >= 0; i--)
        {
            if (!_lines[i].Hidden)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Drops the oldest tenth of the transcript once it exceeds <see cref="MaxLines"/>; rows still on screen stay.</summary>
    protected void Trim()
    {
        if (_lines.Count <= MaxLines)
        {
            return;
        }

        var wanted = _lines.Count / 10;
        var count = 0;
        while (count < wanted && !_lines[count].OnScreen)
        {
            count++;
        }

        if (count > 0)
        {
            _lines.RemoveRange(0, count);
        }
    }

    // ---- what the screen calls ----

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

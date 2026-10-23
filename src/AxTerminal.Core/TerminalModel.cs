using Axit.Core.Transcript;

namespace AxTerminal.Core;

/// <summary>
/// The shell's output as a transcript (SPEC.md AT-4) and the state its integration marks give (AT-1.4): on the
/// bundle's <see cref="LineStore"/>, which holds the lines, the screen, the parser, the bookmarks and the cap. Every
/// row the shell prints is a plain line; the prompt rows, from the A mark to the B mark, are hidden and their text is
/// <see cref="Prompt"/>; a command sent gets its exchange block above the prompt row (AT-3.2); the A mark after a
/// command raises <see cref="CommandFinished"/> with the code the D mark carried (AT-5.1). Without marks there is no
/// state and nothing is hidden. Written against recorded shell sessions, not carved out of Claude's model (AT-D6).
/// </summary>
public sealed class TerminalModel : LineStore
{
    /// <summary>The lines of the current prompt, the A mark's row down to the B mark's row, hidden (AT-4.2); the first is the anchor of a block.</summary>
    private readonly List<Line> _promptLines = [];
    private int _promptStartRow = -1;
    private int _promptRow = -1;
    private string? _lastCode;
    private bool _commandSent;
    private bool _readyRaised;

    public TerminalModel(int columns, int rows)
        : base(columns, rows)
    {
        AttachScreen();
    }

    /// <summary>A mark has arrived from this shell: the state below means something (AT-1.4).</summary>
    public bool HasMarks { get; private set; }

    /// <summary>A command was sent and the prompt has not come back (AT-1.4); false without marks.</summary>
    public bool Running { get; private set; }

    /// <summary>What <see cref="RunningCommand"/> says for a command sent without recording (AT-3.4).</summary>
    public const string UnrecordedCommand = "a command sent without recording";

    /// <summary>The first line of the running command, for the status bar; <see cref="UnrecordedCommand"/> for one sent without recording.</summary>
    public string? RunningCommand { get; private set; }

    public DateTime? RunningSince { get; private set; }

    /// <summary>The last prompt's text, the rows between the A and B marks joined (AT-2.3); null before the first prompt.</summary>
    public string? Prompt { get; private set; }

    /// <summary>The shell's current directory from the 9;9 mark (AT-2.3); null before the first.</summary>
    public string? Directory { get; private set; }

    /// <summary>
    /// What the shell prints at the start of a continuation row of a multi-line command, which the echo hides with
    /// the prompt rows (AT-3.2): PowerShell's <c>&gt;&gt; </c>, bash's <c>&gt; </c>, cmd's <c>More? </c>; null when the
    /// shell has none known, and then no row is taken for one.
    /// </summary>
    public string? ContinuationPrompt { get; set; } = ">> ";

    /// <summary>The cursor row's visible text, for the window's "a program asks" heuristic (AT-5.4); null when the row is a prompt or empty.</summary>
    public string? CursorRowText => _screen.LineAt(_screen.CursorRow) is { Hidden: false, Text.Length: > 0 } line ? line.Text : null;

    /// <summary>The first prompt of a run appeared (AT-5.1, "Ready").</summary>
    public event Action? Ready;

    /// <summary>The prompt came back after a command: the exit code the D mark carried, or null (Command Prompt, or no code).</summary>
    public event Action<int?>? CommandFinished;

    /// <summary>
    /// Records that the user sent a command and prints its exchange block (AT-3.2): the input marker, `## Folder` with
    /// the directory under it (<paramref name="directory"/>, the window's current directory, or the shell's own from
    /// the 9;9 mark when none is given; left out when neither is known), `## Command` with the command's lines under
    /// it, a blank line, the output marker and a blank line, above the prompt row while the shell is idle, at the end
    /// while a command runs (an answer to a program).
    /// Returns false for an empty text, which gets no block and counts as no command (AT-3.1, AT-5.1).
    /// <paramref name="record"/> false sends without a block (AT-3.4).
    /// </summary>
    public bool Send(string text, bool record = true, string? directory = null)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n').Select(l => l.TrimEnd()).ToList();
        while (lines.Count > 0 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        if (lines.Count == 0)
        {
            return false;
        }

        // The block of a command sent to an idle shell goes above the prompt row; an answer to a running program
        // goes at the end, after what the program printed so far.
        var at = _commandSent ? _lines.Count : AnchorIndex();
        if (!_commandSent)
        {
            _commandSent = true;
            Running = HasMarks;
            // AT-3.4: a command sent without recording leaves no trace, the status bar and Ctrl+Q included.
            RunningCommand = record ? lines.First(l => l.Trim().Length > 0).Trim() : UnrecordedCommand;
            RunningSince = Clock();
        }

        if (!record)
        {
            return false;
        }

        ExchangeCount++;
        var stamp = TimeStamps ? $" ({Clock():HH:mm})" : string.Empty;
        var block = new List<Line> { NewLine(LineKind.InputMarker, $"# Input {ExchangeCount}{stamp}", 1) };
        if ((directory ?? Directory) is { } folder)
        {
            block.Add(NewLine(LineKind.UserMessage, "## Folder", 2));
            block.Add(NewLine(LineKind.UserMessage, folder));
        }

        block.Add(NewLine(LineKind.UserMessage, "## Command", 2));
        foreach (var line in lines)
        {
            block.Add(NewLine(LineKind.UserMessage, line));
        }

        block.Add(NewLine(LineKind.UserMessage, string.Empty));
        block.Add(NewLine(LineKind.OutputMarker, $"# Output {ExchangeCount}{stamp}", 1));
        block.Add(NewLine(LineKind.UserMessage, string.Empty));
        _lines.InsertRange(at, block);
        RaiseChanged();
        return true;
    }

    /// <summary>A line of the window's own (<c>System: …</c>, AT-4.3), above the prompt row while the shell is idle.</summary>
    public override void AddSystemLine(string text)
    {
        _lines.Insert(_commandSent ? _lines.Count : AnchorIndex(), NewLine(LineKind.System, "System: " + text));
        RaiseChanged();
    }

    /// <summary>
    /// Empties the window (AT-4.4): every line off the screen goes, the lines still on screen are hidden (the prompt
    /// rows stay what they are), and the blocks and bookmarks count from 1 again. A hidden row that the shell writes
    /// over shows again with its new text (<see cref="TextChanged"/>), so what comes next is seen and what was
    /// there is not; the screen itself is left alone, since its cells are what the shell goes on drawing over.
    /// </summary>
    public void Clear()
    {
        _screen.PullRowText();
        _lines.RemoveAll(l => !l.OnScreen);
        foreach (var line in _lines)
        {
            if (!_promptLines.Contains(line))
            {
                line.ChromeHidden = true;
            }
        }

        ExchangeCount = 0;
        ResetBookmarks();
        RaiseChanged();
    }

    /// <summary>
    /// The shell's process ended (AT-1.7): no prompt will come back, so nothing runs; the system line says so. The
    /// lines stay as they are until a restart (<see cref="ResetScreen"/>).
    /// </summary>
    public void ShellExited(int code)
    {
        _commandSent = false;
        Running = false;
        RunningCommand = null;
        RunningSince = null;
        // At the very end: the prompt row above holds the shell's last words (the exit typed into it).
        _lines.Add(NewLine(LineKind.System, $"System: The shell ended with code {code}"));
        RaiseChanged();
    }

    /// <summary>
    /// Freezes every line still on screen and starts a fresh screen for the next shell in the same window (Restart
    /// shell, AT-1.6): the lines stay, the state is the new shell's.
    /// </summary>
    public void ResetScreen()
    {
        _screen.PullRowText();
        _screen.DetachRows();
        AttachScreen();
        _promptLines.Clear();
        _promptStartRow = _promptRow = -1;
        _lastCode = null;
        _commandSent = false;
        _readyRaised = false;
        HasMarks = false;
        Running = false;
        RunningCommand = null;
        RunningSince = null;
        Prompt = null;
        RaiseChanged();
    }

    protected override void TextChanged(Line line, string text)
    {
        base.TextChanged(line, text);
        if (line.ChromeHidden && !_promptLines.Contains(line))
        {
            // A hidden row written over: an old prompt row after cls, or a row Clear hid, filled with output again.
            line.ChromeHidden = false;
        }
    }

    // ---- internals ----

    /// <summary>The base's fresh screen with the shell's handlers on it: the marks, the directory, and the rows let go on cls.</summary>
    private void AttachScreen()
    {
        var screen = NewScreen();
        screen.Mark += OnMark;
        screen.DirectoryChanged += d => Directory = d;
        screen.Homed += LetRowsGo;
        screen.Cleared += LetRowsGo;
    }

    /// <summary>
    /// The cursor went to the top left corner, or the screen is being erased: cls, or a program starting the screen
    /// afresh. The rows let go of their lines, which keep the text they had (AT-4.4), so that what is printed on the
    /// rows next gets lines of its own at the end of the transcript instead of landing on the lines the rows had at
    /// the top of it, and so that the erase blanks no line already read.
    /// </summary>
    private void LetRowsGo()
    {
        for (var r = 0; r < Rows; r++)
        {
            if (_screen.LineAt(r) is not null)
            {
                _screen.PullRowText();
                _screen.DetachRows();
                return;
            }
        }
    }

    private void OnMark(char letter, string? code)
    {
        HasMarks = true;
        switch (letter)
        {
            case 'D':
                _lastCode = code;
                break;
            case 'A':
                _promptStartRow = _screen.CursorRow;
                if (_commandSent)
                {
                    _commandSent = false;
                    Running = false;
                    RunningCommand = null;
                    RunningSince = null;
                    CommandFinished?.Invoke(int.TryParse(_lastCode, out var value) ? value : null);
                }
                else if (!_readyRaised)
                {
                    _readyRaised = true;
                    Ready?.Invoke();
                }

                _lastCode = null;
                break;
            case 'B':
                if (_promptStartRow < 0)
                {
                    // A B without an A since the last prompt (a program inside the shell that marks its own input,
                    // Claude Code among them): no prompt rows to hide. Taken for a prompt, it hid the whole screen.
                    break;
                }

                _screen.PullRowText();
                // The continuation rows of the command just run sit under the old prompt row: hidden before it is forgotten.
                HideContinuationRows();
                _promptLines.Clear();
                _promptRow = _screen.CursorRow;
                var texts = new List<string>();
                for (var r = _promptStartRow; r <= _promptRow; r++)
                {
                    if (_screen.LineAt(r) is { } line)
                    {
                        line.ChromeHidden = true;
                        _promptLines.Add(line);
                        if (line.Text.Length > 0)
                        {
                            texts.Add(line.Text);
                        }
                    }
                }

                _promptStartRow = -1;
                Prompt = string.Join(" ", texts);
                break;
        }
    }

    protected override void CompleteFrame()
    {
        _screen.PullRowText();
        HideContinuationRows();
        if (JoinWrappedLines)
        {
            JoinWrappedRows();
        }

        Trim();
        RaiseChanged();
    }

    /// <summary>
    /// The continuation rows of a multi-line command (the shell's <see cref="ContinuationPrompt"/>: PowerShell's
    /// <c>&gt;&gt; </c>, bash's <c>&gt; </c>) are the echo's too: hidden while they sit directly under the prompt row
    /// or under another hidden continuation row (AT-3.2).
    /// </summary>
    private void HideContinuationRows()
    {
        if (_promptLines.Count == 0 || _promptRow < 0 || ContinuationPrompt is null)
        {
            return;
        }

        var bare = ContinuationPrompt.TrimEnd();
        for (var r = _promptRow + 1; r < Rows; r++)
        {
            var line = _screen.LineAt(r);
            if (line is null || !line.Text.StartsWith(ContinuationPrompt, StringComparison.Ordinal) && line.Text != bare)
            {
                return;
            }

            if (!line.ChromeHidden)
            {
                line.ChromeHidden = true;
                _promptLines.Add(line);
            }
        }
    }

    /// <summary>A row that fills every column is hard-wrapped by the program; the next row with text continues it (AT-4.1).</summary>
    private void JoinWrappedRows()
    {
        for (var r = 0; r + 1 < Rows; r++)
        {
            var line = _screen.LineAt(r);
            var next = _screen.LineAt(r + 1);
            if (line is { Hidden: false } && next is { Hidden: false, Text.Length: > 0 } && line.Text.Length == Columns)
            {
                next.JoinedToPrevious = true;
            }
        }
    }

    /// <summary>Where a block or a system line goes while the shell is idle: above the prompt rows, or at the end without any.</summary>
    private int AnchorIndex()
    {
        if (_promptLines.Count > 0 && _promptLines[0].OnScreen)
        {
            var index = _lines.LastIndexOf(_promptLines[0]);
            if (index >= 0)
            {
                return index;
            }
        }

        return _lines.Count;
    }

}

using Axit.Core.Transcript;

namespace AxTerminal.Core;

/// <summary>What the shell's marks say about it (SPEC.md AT-1.4): no mark yet (no state at all), at its prompt, or running a command.</summary>
public enum ShellState
{
    /// <summary>No mark has arrived from this shell: nothing is hidden and nothing is announced (a custom shell, a shell not ready yet).</summary>
    NoMarks,

    /// <summary>The prompt is up and nothing was sent since.</summary>
    Idle,

    /// <summary>A command was sent and the prompt has not come back.</summary>
    Running,
}

/// <summary>
/// The shell's output as a transcript (SPEC.md AT-4) and the state its integration marks give (AT-1.4): on the
/// bundle's <see cref="LineStore"/>, which holds the lines, the screen, the parser, the bookmarks and the cap. Every
/// row the shell prints is a plain line; the prompt rows, from the A mark to the B mark, are hidden and their text is
/// <see cref="Prompt"/>; a command sent gets its exchange block above the prompt row (AT-3.2); the A mark after a
/// command raises <see cref="CommandFinished"/> with the code the D mark carried (AT-5.1). Without marks there is no
/// state and nothing is hidden. The prompt rows are found by the lines the screen binds to its rows, never by a row
/// number kept past the frame (2026-10-23; by row number until then, three bugs came of rows that had scrolled).
/// Written against recorded shell sessions, not carved out of Claude's model (AT-D6).
/// </summary>
public sealed class TerminalModel : LineStore
{
    /// <summary>The lines of the current prompt, the A mark's row down to the B mark's row, and the continuation rows of the command after it, hidden (AT-4.2); the first is the anchor of a block.</summary>
    private readonly List<Line> _promptLines = [];

    /// <summary>The line of the row the A mark landed on, until the B mark closes the prompt; null between prompts.</summary>
    private Line? _promptStart;

    /// <summary>The lines of the last command as sent (trimmed), which the continuation rows under the prompt echo (AT-3.2); kept until the next prompt's B mark, emptied then.</summary>
    private readonly List<string> _sentLines = [];

    /// <summary>The code the last D mark carried, for the A mark that follows it.</summary>
    private string? _lastCode;

    public TerminalModel(int columns, int rows)
        : base(columns, rows)
    {
        AttachScreen();
    }

    /// <summary>What the marks say the shell is doing (AT-1.4): the one state behind <see cref="HasMarks"/> and <see cref="Running"/>.</summary>
    public ShellState State { get; private set; }

    /// <summary>A mark has arrived from this shell: the state means something (AT-1.4).</summary>
    public bool HasMarks => State != ShellState.NoMarks;

    /// <summary>A command was sent and the prompt has not come back (AT-1.4); false without marks.</summary>
    public bool Running => State == ShellState.Running;

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
        var at = State == ShellState.Running ? _lines.Count : AnchorIndex();
        if (State == ShellState.Idle)
        {
            State = ShellState.Running;
            // AT-3.4: a command sent without recording leaves no trace, the status bar and Ctrl+Q included.
            RunningCommand = record ? lines.First(l => l.Trim().Length > 0).Trim() : UnrecordedCommand;
            RunningSince = Clock();
            _sentLines.Clear();
            _sentLines.AddRange(lines.Select(l => l.Trim()));
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
        _lines.Insert(State == ShellState.Running ? _lines.Count : AnchorIndex(), NewLine(LineKind.System, "System: " + text));
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
        if (State == ShellState.Running)
        {
            State = ShellState.Idle;
        }

        RunningCommand = null;
        RunningSince = null;
        _sentLines.Clear();
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
        _promptStart = null;
        _sentLines.Clear();
        _lastCode = null;
        State = ShellState.NoMarks;
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

    /// <summary>
    /// A shell integration mark (AT-1.3). D keeps its code for the A that follows. A ends a running command, or raises
    /// Ready once, and marks the cursor row's line as the start of the prompt (the line is made now, before the prompt
    /// is printed on it, so that the row is found again by identity whatever scrolls in between). B closes the prompt:
    /// the rows from that line's row to the cursor row are hidden and their text becomes <see cref="Prompt"/>.
    /// </summary>
    private void OnMark(char letter, string? code)
    {
        switch (letter)
        {
            case 'D':
                _lastCode = code;
                break;
            case 'A':
                _promptStart = _screen.EnsureLine(_screen.CursorRow);
                if (State == ShellState.Running)
                {
                    // The sent lines stay until the B mark, which hides the continuation rows under the old prompt first.
                    State = ShellState.Idle;
                    RunningCommand = null;
                    RunningSince = null;
                    CommandFinished?.Invoke(int.TryParse(_lastCode, out var value) ? value : null);
                }
                else if (State == ShellState.NoMarks)
                {
                    State = ShellState.Idle;
                    Ready?.Invoke();
                }

                _lastCode = null;
                break;
            case 'B':
                if (_promptStart is null)
                {
                    // A B without an A since the last prompt (a program inside the shell that marks its own input,
                    // Claude Code among them): no prompt rows to hide. Taken for a prompt, it hid the whole screen.
                    State = State == ShellState.NoMarks ? ShellState.Idle : State;
                    break;
                }

                _screen.PullRowText();
                // The continuation rows of the command just run sit under the old prompt row: hidden before it is
                // forgotten, whether or not a frame ended since they were printed (a burst can hold them and the new prompt).
                HideContinuationRows();
                _sentLines.Clear();
                _promptLines.Clear();
                var first = Math.Max(0, _screen.RowOf(_promptStart));
                var texts = new List<string>();
                for (var r = first; r <= _screen.CursorRow; r++)
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

                _promptStart = null;
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
    /// The continuation rows of a multi-line command are the echo's too (AT-3.2): while the command runs, the rows
    /// directly under the prompt rows (found by their lines, wherever the rows are now) whose text is the shell's
    /// <see cref="ContinuationPrompt"/> followed by one of the command's lines, or by nothing, are hidden with them.
    /// An output line that happens to start with the continuation prompt is none of the command's lines and stays.
    /// </summary>
    private void HideContinuationRows()
    {
        if (_promptLines.Count == 0 || ContinuationPrompt is null || _sentLines.Count == 0)
        {
            return;
        }

        var row = _screen.RowOf(_promptLines[^1]);
        if (row < 0)
        {
            return;
        }

        var bare = ContinuationPrompt.TrimEnd();
        for (var r = row + 1; r < Rows; r++)
        {
            var line = _screen.LineAt(r);
            if (line is null || !IsContinuationRow(line.Text, bare))
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

    /// <summary>Whether a row's text is the continuation prompt with one of the sent lines, or the bare prompt (the host's empty continuation after the last line).</summary>
    private bool IsContinuationRow(string text, string bare)
    {
        if (text == bare)
        {
            return true;
        }

        if (!text.StartsWith(ContinuationPrompt!, StringComparison.Ordinal))
        {
            return false;
        }

        var rest = text[ContinuationPrompt!.Length..].Trim();
        return rest.Length == 0 || _sentLines.Contains(rest);
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

using Axit.Core;
using Axit.Core.Transcript;
using Axit.Forms;

namespace AxTerminal;

/// <summary>
/// AxTerminal's window-specific keys (SPEC.md AT-6.2; bundle AX-8). A key is changed here and nowhere else: the
/// window's key switch, the menu items and the terminal's quick keys (<see cref="Transcript"/>) read this table, and
/// the F1 text in <c>HelpText</c> names the same keys. The bundle-wide keys (F1, the controls, find, save, text size,
/// the page and paragraph keys) are <c>Axit.Forms.BundleKeys</c>; the collision test in <c>tests/Axit.Tests</c> fails
/// when a key here equals one of those or when a key appears twice.
/// </summary>
public static class AxTerminalKeys
{
    // Anywhere in the window.
    public const Keys GoToField = Keys.Control | Keys.D1;
    public const Keys GoToTerminal = Keys.Control | Keys.D2;
    /// <summary>Navigate → Latest output: the newest output marker, from anywhere, the third place after the field and the terminal (AT-2.2).</summary>
    public const Keys LatestOutput = Keys.Control | Keys.D3;
    /// <summary>Escape to the shell (AT-3.5); Ctrl+Escape is Windows' own.</summary>
    public const Keys SendEscape = Keys.Shift | Keys.Escape;
    /// <summary>Ctrl+C to the shell, the interrupt (AT-3.5); plain Ctrl+C copies.</summary>
    public const Keys Interrupt = Keys.Control | Keys.Shift | Keys.C;
    /// <summary>Ctrl+Alt with a quick key letter, from anywhere: every line of that kind from the caret's line to the end, spoken once (AxClaude D36).</summary>
    public const Keys ReadToEnd = Keys.Control | Keys.Alt;
    public const Keys BookmarkLine = Keys.Control | Keys.Shift | Keys.K;
    public const Keys ShowInExplorer = Keys.Control | Keys.W;
    public const Keys NewSession = Keys.Control | Keys.N;
    public const Keys RestartShell = Keys.Control | Keys.Shift | Keys.R;
    public const Keys ClearTranscript = Keys.Control | Keys.L;
    public const Keys RecentCommands = Keys.Control | Keys.R;
    /// <summary>What the window is doing: the state, the command, how long, the prompt (AT-2.3, AT-D8).</summary>
    public const Keys WhatIsRunning = Keys.Control | Keys.Q;
    /// <summary>The current folder, the full path, on demand (AT-D9).</summary>
    public const Keys SayFolder = Keys.Control | Keys.P;
    /// <summary>Background music on or off, said each time (AT-5.5).</summary>
    public const Keys ToggleMusic = Keys.Control | Keys.Space;

    // The command field, and the terminal: Enter sends a command in the field and copies the line in the terminal;
    // Escape is the guard in the field and clears the selection or returns to the field in the terminal.
    public const Keys Enter = Keys.Return;
    /// <summary>In the command field: the text goes to the shell with no block, no history, no log (AT-3.4).</summary>
    public const Keys SendWithoutRecording = Keys.Control | Keys.Return;
    public const Keys Escape = Keys.Escape;

    /// <summary>In the terminal with a selection: copies it and says so; the control's own copy otherwise.</summary>
    public const Keys CopySelection = Keys.Control | Keys.C;

    // The terminal's quick keys (AxClaude FR-5): a letter alone, Shift with it for the previous one.
    public const Keys Heading = Keys.H;
    public const Keys Input = Keys.I;
    public const Keys Output = Keys.O;
    public const Keys Paragraph = Keys.P;
    public const Keys SystemLine = Keys.S;
    public const Keys Bookmark = Keys.K;
    public const Keys BookmarkToggle = Keys.M;
    public const Keys LineNumber = Keys.L;
    public const Keys JumpBack = Keys.Back;
    public const Keys HeadingLevel1 = Keys.D1;
    public const Keys HeadingLevel2 = Keys.D2;
    public const Keys HeadingLevel3 = Keys.D3;
    public const Keys HeadingLevel4 = Keys.D4;
    public const Keys HeadingLevel5 = Keys.D5;
    public const Keys HeadingLevel6 = Keys.D6;

    /// <summary>Every key above with its name, for the collision test.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Go to the command field", GoToField),
        ("Go to the terminal", GoToTerminal),
        ("Latest output", LatestOutput),
        ("Send Escape to the shell", SendEscape),
        ("Interrupt (Ctrl+C to the shell)", Interrupt),
        ("Read to the end: Ctrl+Alt with a quick key letter", ReadToEnd),
        ("Bookmark this line", BookmarkLine),
        ("Show the folder in File Explorer", ShowInExplorer),
        ("New session", NewSession),
        ("Restart shell", RestartShell),
        ("Clear transcript", ClearTranscript),
        ("Recent commands", RecentCommands),
        ("What is running", WhatIsRunning),
        ("Say current folder", SayFolder),
        ("Background music on or off", ToggleMusic),
        ("Enter: send, or copy the line", Enter),
        ("Send without recording", SendWithoutRecording),
        ("Escape: the guard, clear the selection or go to the field", Escape),
        ("Copy the selection", CopySelection),
        ("Heading", Heading),
        ("Input", Input),
        ("Output", Output),
        ("Paragraph", Paragraph),
        ("System line", SystemLine),
        ("Bookmark", Bookmark),
        ("Bookmark this line (m)", BookmarkToggle),
        ("Line number", LineNumber),
        ("Back to the line before the jump", JumpBack),
        ("Heading level 1", HeadingLevel1),
        ("Heading level 2", HeadingLevel2),
        ("Heading level 3", HeadingLevel3),
        ("Heading level 4", HeadingLevel4),
        ("Heading level 5", HeadingLevel5),
        ("Heading level 6", HeadingLevel6),
    ];

    /// <summary>k and Shift+K: the bookmarks dropped with m; the Navigate menu reaches them too.</summary>
    private static readonly QuickKey BookmarkKey = new(Bookmark, "bookmark", l => l.Kind == LineKind.Bookmark);

    /// <summary>o and Shift+O: the output markers; Ctrl+3 goes to the newest one.</summary>
    private static readonly QuickKey OutputKey = new(Output, "output", l => l.Kind == LineKind.OutputMarker);

    /// <summary>
    /// The terminal's keys for the bundle's transcript view (SPEC.md AT-6.2): the kinds the terminal has, as data. A
    /// shell's rows are plain lines, so the kinds are the window's own: the markers, headings (the markers are level
    /// 1), paragraphs, system lines and bookmarks.
    /// </summary>
    public static readonly TranscriptKeys Transcript = new(
        QuickKeys:
        [
            new(Heading, "heading", l => l.HeadingLevel > 0),
            new(Input, "input", l => l.Kind == LineKind.InputMarker),
            OutputKey,
            // A paragraph is a line with text after a blank line, or the first line (AxClaude FR-5's rule without Claude's kinds).
            new(Paragraph, "paragraph", l => l.Text.Length > 0, Previous: (_, p) => p.Text.Length == 0, ReadsWhole: true),
            new(SystemLine, "system line", l => l.Kind == LineKind.System),
            BookmarkKey,
            new(HeadingLevel1, "heading level 1", l => l.HeadingLevel == 1),
            new(HeadingLevel2, "heading level 2", l => l.HeadingLevel == 2),
            new(HeadingLevel3, "heading level 3", l => l.HeadingLevel == 3),
            new(HeadingLevel4, "heading level 4", l => l.HeadingLevel == 4),
            new(HeadingLevel5, "heading level 5", l => l.HeadingLevel == 5),
            new(HeadingLevel6, "heading level 6", l => l.HeadingLevel == 6),
        ],
        Bookmark: BookmarkKey,
        Response: OutputKey,
        BookmarkToggle: BookmarkToggle,
        LineNumber: LineNumber,
        JumpBack: JumpBack,
        Enter: Enter,
        Escape: Escape,
        CopySelection: CopySelection,
        BookmarkLine: BookmarkLine,
        ReadToEnd: ReadToEnd);
}

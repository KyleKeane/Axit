namespace AxClaude;

/// <summary>
/// AxClaude's window-specific keys (SPEC.md §6.3; bundle AX-8). A key is changed here and nowhere else: the window's
/// key switch, the menu items and the conversation's quick keys read this table, and the F1 text in <c>HelpText</c>
/// names the same keys. The bundle-wide keys (F1, the controls, find, save, text size, the page keys) are
/// <c>Axit.Forms.BundleKeys</c>; the collision test in <c>tests/Axit.Tests</c> fails when a key here equals one of those
/// or when a key appears twice.
/// </summary>
public static class AxClaudeKeys
{
    // Anywhere in the window.
    public const Keys GoToField = Keys.Control | Keys.D1;
    public const Keys GoToConversation = Keys.Control | Keys.D2;
    public const Keys Interrupt = Keys.Shift | Keys.Escape;
    public const Keys SendNow = Keys.Control | Keys.Shift | Keys.S;
    public const Keys SendCtrlC = Keys.Control | Keys.Shift | Keys.C;
    public const Keys NextPermissionMode = Keys.Control | Keys.Shift | Keys.M;
    public const Keys LatestResponse = Keys.Control | Keys.Shift | Keys.O;
    public const Keys BookmarkLine = Keys.Control | Keys.Shift | Keys.K;
    public const Keys OpenFolder = Keys.Control | Keys.W;
    public const Keys NewSession = Keys.Control | Keys.N;
    public const Keys RestartClaude = Keys.Control | Keys.Shift | Keys.R;

    /// <summary>D14: never sent to Claude; the key only says why.</summary>
    public const Keys CtrlOGuard = Keys.Control | Keys.O;

    // The message field, and the conversation: Enter sends a message in the field and quotes the line in the
    // conversation; Escape is the guard in the field (D20) and clears the selection or returns to the field in the
    // conversation.
    public const Keys Enter = Keys.Return;
    public const Keys Escape = Keys.Escape;
    public const Keys SendUp = Keys.Control | Keys.Up;
    public const Keys SendDown = Keys.Control | Keys.Down;

    // The conversation's quick keys (FR-5): a letter alone, Shift with it for the previous one.
    public const Keys Heading = Keys.H;
    public const Keys Input = Keys.I;
    public const Keys Response = Keys.O;
    public const Keys ResponseAlso = Keys.R;
    public const Keys ClaudeReply = Keys.C;
    public const Keys ToolLine = Keys.T;
    public const Keys Paragraph = Keys.P;
    public const Keys Error = Keys.E;
    public const Keys TurnSummary = Keys.D;
    public const Keys SystemLine = Keys.S;
    public const Keys Question = Keys.Q;
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
        ("Go to the message field", GoToField),
        ("Go to the conversation", GoToConversation),
        ("Interrupt Claude", Interrupt),
        ("Send the waiting message now", SendNow),
        ("Send Ctrl+C", SendCtrlC),
        ("Next permission mode", NextPermissionMode),
        ("Latest response", LatestResponse),
        ("Bookmark this line", BookmarkLine),
        ("Open the current folder", OpenFolder),
        ("New session", NewSession),
        ("Force Claude to restart", RestartClaude),
        ("Ctrl+O guard", CtrlOGuard),
        ("Enter: send, or quote the line", Enter),
        ("Escape: the guard, clear the selection or go to the field", Escape),
        ("Send Up", SendUp),
        ("Send Down", SendDown),
        ("Heading", Heading),
        ("Input", Input),
        ("Response", Response),
        ("Response (r)", ResponseAlso),
        ("Claude reply", ClaudeReply),
        ("Tool line", ToolLine),
        ("Paragraph", Paragraph),
        ("Error", Error),
        ("Turn summary", TurnSummary),
        ("System line", SystemLine),
        ("Question", Question),
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
}

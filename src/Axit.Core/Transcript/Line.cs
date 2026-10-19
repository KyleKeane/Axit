namespace Axit.Core.Transcript;

public enum LineKind
{
    Plain,
    InputMarker,
    OutputMarker,
    System,
    UserEcho,
    ClaudeReply,
    Thinking,
    Tool,
    ToolError,
    Error,
    Warning,
    Prompt,
    PromptOption,
    TurnSummary,

    /// <summary>A line of the user's message as the app printed it between the markers, or one of the block's blank lines (FR-4.1).</summary>
    UserMessage,

    /// <summary>A bookmark the reader dropped in front of a line with m: <c>Bookmark 3</c>, found again with k (FR-3.9).</summary>
    Bookmark,

    /// <summary>
    /// The app's record of a question or screen Claude waited on, <c>Question: Select model</c>, in front of its first row
    /// and found again with q (D33). Claude redraws an answered question as a one-line result, so this is what stays.
    /// </summary>
    Question,
}

/// <summary>
/// One logical line of the transcript. Screen rows keep a reference to their line while they are on screen,
/// so a row that is rewritten updates its line instead of adding a new one. Marker and system lines are
/// created by the app and have no row.
/// </summary>
public sealed class Line
{
    public Line(int id, LineKind kind, string text)
    {
        Id = id;
        Kind = kind;
        Text = text;
    }

    public int Id { get; }
    public string Text { get; set; }
    public LineKind Kind { get; set; }
    public int HeadingLevel { get; set; }

    private string? _bare;
    private string? _bareOf;
    private int _bareLevel;

    /// <summary>
    /// The text without its Markdown heading marker (<c>## Folder</c> as <c>Folder</c>), for a view whose markers are
    /// hidden (AxClaude FR-4.6a): the text itself for a line that is no heading, and the same object while the text
    /// and the level are unchanged, so that a mirror can tell a changed line by reference.
    /// </summary>
    public string BareText
    {
        get
        {
            if (!ReferenceEquals(_bareOf, Text) || _bareLevel != HeadingLevel)
            {
                _bareOf = Text;
                _bareLevel = HeadingLevel;
                _bare = HeadingLevel > 0 && Text.StartsWith('#') ? Text.TrimStart('#').TrimStart(' ') : Text;
            }

            return _bare!;
        }
    }

    /// <summary>
    /// The row continues a reply row that Claude hard-wrapped at the console width (FR-3.2a): the view shows the two
    /// as one line, joined with a space.
    /// </summary>
    public bool JoinedToPrevious { get; set; }

    public bool Committed { get; set; }
    public bool Hidden => ChromeHidden || EchoHidden || ReplayHidden || RepaintHidden;
    /// <summary>
    /// A line of the app's own (a marker, a printed message line or blank line, a system line, a bookmark, the record
    /// of a question), plus Claude's one <c>[Screen Reader Mode: …]</c> row, which is classified System as well: not
    /// classified again by text, no stop for the tick, no row to join.
    /// </summary>
    public bool IsMarker => Kind is LineKind.InputMarker or LineKind.OutputMarker or LineKind.System or LineKind.UserMessage or LineKind.Bookmark or LineKind.Question;

    public bool ChromeHidden { get; set; }
    public string? StickyChromeText { get; set; }
    public bool EchoHidden { get; set; }
    public bool EchoHandled { get; set; }
    /// <summary>Printed while a restart replayed a conversation the window already shows (FR-4.8).</summary>
    public bool ReplayHidden { get; set; }

    /// <summary>
    /// Printed while Claude repainted the whole conversation from the top of the screen: a copy of what the window
    /// already shows, often worded differently. Shown once a later frame writes something else on its row.
    /// </summary>
    public bool RepaintHidden { get; set; }

    /// <summary>The line is a row of the screen still, so it may be rewritten; false once its row scrolled off or was let go.</summary>
    public bool OnScreen => Row is not null;

    internal Vt.Row? Row { get; set; }

    public override string ToString() => $"{Id}: [{Kind}] {Text}";
}

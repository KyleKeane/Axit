using Axit.Core;
using Axit.Forms;

namespace AxPreview;

/// <summary>
/// axPreview's window-specific keys (SPEC.md AP-3.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the key handling in the editor shell and the F1 text read this table. The viewer's keys of the shell (new
/// window, open, reload, edit in the editor, show in File Explorer, go to line, word wrap, the line number) are
/// <c>Axit.Forms.EditorKeys.Viewer</c>, and <see cref="All"/> includes them; the navigation letters are axDown's
/// (docs/axdown/SPEC.md §9), the same values, so that a file read here and edited there is under the same keys (G2);
/// the bundle-wide keys (F1, find, Ctrl+S, text size, the controls) are <c>Axit.Forms.BundleKeys</c>.
/// </summary>
public static class AxPreviewKeys
{
    public const Keys NextHeading = Keys.Control | Keys.H;
    public const Keys PreviousHeading = Keys.Control | Keys.Shift | Keys.H;

    /// <summary>Ctrl+1 to Ctrl+6: the next heading of that level; Ctrl+Shift+1 to Ctrl+Shift+6: the previous one.</summary>
    public static Keys NextHeadingLevel(int level) => Keys.Control | (Keys.D0 + level);

    public static Keys PreviousHeadingLevel(int level) => Keys.Control | Keys.Shift | (Keys.D0 + level);

    public const Keys NextLink = Keys.Control | Keys.K;
    public const Keys PreviousLink = Keys.Control | Keys.Shift | Keys.K;
    public const Keys NextList = Keys.Control | Keys.L;
    public const Keys PreviousList = Keys.Control | Keys.Shift | Keys.L;
    public const Keys NextListItem = Keys.Control | Keys.I;
    public const Keys PreviousListItem = Keys.Control | Keys.Shift | Keys.I;
    public const Keys NextBlockQuote = Keys.Control | Keys.Q;
    public const Keys PreviousBlockQuote = Keys.Control | Keys.Shift | Keys.Q;
    public const Keys NextTable = Keys.Control | Keys.T;
    public const Keys PreviousTable = Keys.Control | Keys.Shift | Keys.T;

    /// <summary>Every key of the window with its name, the shell's viewer keys first, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ..EditorKeys.Viewer,
        ("Next heading", NextHeading),
        ("Previous heading", PreviousHeading),
        ..Enumerable.Range(1, 6).Select(level => ($"Next heading level {level}", NextHeadingLevel(level))),
        ..Enumerable.Range(1, 6).Select(level => ($"Previous heading level {level}", PreviousHeadingLevel(level))),
        ("Next link", NextLink),
        ("Previous link", PreviousLink),
        ("Next list", NextList),
        ("Previous list", PreviousList),
        ("Next list item", NextListItem),
        ("Previous list item", PreviousListItem),
        ("Next block quote", NextBlockQuote),
        ("Previous block quote", PreviousBlockQuote),
        ("Next table", NextTable),
        ("Previous table", PreviousTable),
    ];

    /// <summary>The navigation keys as pairs, one per kind: the shell handles them through this list and the notice text takes the same list (bundle AX-7.2).</summary>
    public static readonly IReadOnlyList<TextJump> Jumps =
    [
        TextJump.Markdown(NextHeading, PreviousHeading, ElementKind.Heading),
        ..Enumerable.Range(1, 6).Select(level => TextJump.Markdown(NextHeadingLevel(level), PreviousHeadingLevel(level), ElementKind.Heading, level)),
        TextJump.Markdown(NextLink, PreviousLink, ElementKind.Link),
        TextJump.Markdown(NextList, PreviousList, ElementKind.List),
        TextJump.Markdown(NextListItem, PreviousListItem, ElementKind.ListItem),
        TextJump.Markdown(NextBlockQuote, PreviousBlockQuote, ElementKind.BlockQuote),
        TextJump.Markdown(NextTable, PreviousTable, ElementKind.Table),
    ];
}

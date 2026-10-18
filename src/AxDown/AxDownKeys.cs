using Axit.Core;
using Axit.Forms;

namespace AxDown;

/// <summary>
/// AxDown's window-specific keys (SPEC.md AD-5; bundle AX-8). A key is changed here and nowhere else: the menu items,
/// the key handling and the F1 text read this table. The editor shell's keys (new, open, save as, go to line, word
/// wrap, show in File Explorer) are <c>Axit.Forms.EditorKeys</c>, the same in every editor window, and <see cref="All"/>
/// includes them; the bundle-wide keys (F1, find, save, text size, the controls) are <c>Axit.Forms.BundleKeys</c>. The
/// collision test in <c>tests/Axit.Tests</c> fails when a key here equals one of those.
/// </summary>
public static class AxDownKeys
{
    // Navigation (SPEC.md §9, AD-D7): NVDA's browse-mode letters with Ctrl for the next item, Ctrl+Shift for the previous.
    public const Keys NextHeading = Keys.Control | Keys.H;
    public const Keys PreviousHeading = Keys.Control | Keys.Shift | Keys.H;

    /// <summary>Ctrl+1 to Ctrl+6: the next heading of that level; Ctrl+Shift+1 to Ctrl+Shift+6: the previous one (SPEC.md §9).</summary>
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

    /// <summary>Every key of the window with its name, the editor shell's first, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ..EditorKeys.All,
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

    /// <summary>The keys of SPEC.md §9 as pairs, one per kind: the editor handles them through this list, and the notice text takes the same list (bundle AX-7.2).</summary>
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

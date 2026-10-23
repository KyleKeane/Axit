using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// A window's key pair for one kind of element in a text (docs/axit/SPEC.md AX-7.2): the key for the next one, the key
/// for the previous one, the kind's name as spoken ("heading level 2", "key"), and the two functions that find the next
/// and the previous element of the kind from a caret position in a text. <see cref="Markdown"/> makes the pair for a
/// Markdown kind from <see cref="MarkdownOutline"/>; another outline, JSON's, brings its own functions. Each window
/// builds its list from its own key table and hands it to <see cref="OverlayPanel.Jumps"/> and, in an editor, to its
/// own text, so that a notice follows the window's convention.
/// </summary>
public sealed record TextJump(Keys Next, Keys Previous, string Name, Func<string, int, Element?> NextOf, Func<string, int, Element?> PreviousOf)
{
    /// <summary>
    /// The pairs every notice's text takes (AX-7.2), the letters of AxClaude's transcript: h and Shift+H for the next
    /// and the previous heading, 1 to 6 and Shift with them for one level. A read-only text has no use for typed
    /// letters, so they are free in every window; an editor's notices take these and the editor's own Ctrl+letter pairs.
    /// </summary>
    public static readonly IReadOnlyList<TextJump> HeadingLetters =
    [
        Markdown(Keys.H, Keys.Shift | Keys.H, ElementKind.Heading),
        .. Enumerable.Range(1, 6).Select(level => Markdown(Keys.D0 + level, Keys.Shift | (Keys.D0 + level), ElementKind.Heading, level)),
    ];

    /// <summary>The pair for a Markdown kind, with the level narrowing headings (0 for any).</summary>
    public static TextJump Markdown(Keys next, Keys previous, ElementKind kind, int level = 0) =>
        new(next, previous, MarkdownOutline.Name(kind, level),
            (text, caret) => MarkdownOutline.Next(kind, text, caret, level),
            (text, caret) => MarkdownOutline.Previous(kind, text, caret, level));
}

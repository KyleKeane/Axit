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
    /// The pairs every notice's text takes (AX-7.2), the letters of axClaude's transcript: h and Shift+H for the next
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

    // ---- the jump itself (was TextJumps, one type since 2026-10-25, inventory item 11) ----
    // Moving through an edit control's text by its structure (docs/axit/SPEC.md AX-7.2, axDown SPEC §9), the one
    // implementation for every editor window and for every notice's text: the key pairs come from the window's table
    // (<see cref="TextJump"/>), the elements from the pair's own outline, and the result is one announcement, the
    // element's text, or "No next heading" and the like with the caret staying. Tested in <c>tests/Axit.Tests</c>.

    /// <summary>Acts on the key when it is one of the pairs and says so; false when it is none of them.</summary>
    public static bool Handle(TextBoxBase text, IReadOnlyList<TextJump> jumps, Keys key, Func<string, bool, bool> announce)
    {
        foreach (var jump in jumps)
        {
            if (key == jump.Next)
            {
                Jump(text, jump, 1, announce);
                return true;
            }

            if (key == jump.Previous)
            {
                Jump(text, jump, -1, announce);
                return true;
            }
        }

        return false;
    }

    /// <summary>The caret to the start of the next (direction 1) or previous (-1) element of the pair's kind, spoken.</summary>
    public static void Jump(TextBoxBase text, TextJump jump, int direction, Func<string, bool, bool> announce)
    {
        var element = direction > 0
            ? jump.NextOf(text.Text, text.SelectionStart)
            : jump.PreviousOf(text.Text, text.SelectionStart);
        if (element is null)
        {
            announce($"No {(direction > 0 ? "next" : "previous")} {jump.Name}", true);
            return;
        }

        text.Select(element.Start, 0);
        text.ScrollToCaret();
        announce(element.Spoken, true);
    }

    /// <summary>
    /// The menu item text for a pair, "&amp;Heading", "Heading level &amp;1", "Lin&amp;k": the kind's name with the
    /// key's own letter or digit as the mnemonic.
    /// </summary>
    public static string MenuLabel(TextJump jump)
    {
        var name = jump.Name;
        var letter = BundleKeys.Text(jump.Next & Keys.KeyCode)[0];
        var at = name.IndexOf(letter, StringComparison.OrdinalIgnoreCase);
        var label = char.ToUpperInvariant(name[0]) + name[1..];
        return at < 0 ? "&" + label : label.Insert(at, "&");
    }
}

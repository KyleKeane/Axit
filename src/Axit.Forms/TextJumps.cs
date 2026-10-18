using Axit.Core;

namespace Axit.Forms;

/// <summary>
/// Moving through an edit control's text by Markdown structure (docs/axit/SPEC.md AX-7.2, AxDown SPEC §9), the one
/// implementation for AxDown's editor and for every notice's text: the key pairs come from the window's table
/// (<see cref="TextJump"/>), the elements from <see cref="MarkdownOutline"/>, and the result is one announcement, the
/// element's text, or "No next heading" and the like with the caret staying. Tested in <c>tests/Axit.Tests</c>.
/// </summary>
public static class TextJumps
{
    /// <summary>Acts on the key when it is one of the pairs and says so; false when it is none of them.</summary>
    public static bool Handle(TextBoxBase text, IReadOnlyList<TextJump> jumps, Keys key, Func<string, bool, bool> announce)
    {
        foreach (var jump in jumps)
        {
            if (key == jump.Next)
            {
                Jump(text, jump.Kind, 1, jump.Level, announce);
                return true;
            }

            if (key == jump.Previous)
            {
                Jump(text, jump.Kind, -1, jump.Level, announce);
                return true;
            }
        }

        return false;
    }

    /// <summary>The caret to the start of the next (direction 1) or previous (-1) element of the kind, spoken; the level narrows headings, 0 for any.</summary>
    public static void Jump(TextBoxBase text, ElementKind kind, int direction, int level, Func<string, bool, bool> announce)
    {
        var element = direction > 0
            ? MarkdownOutline.Next(kind, text.Text, text.SelectionStart, level)
            : MarkdownOutline.Previous(kind, text.Text, text.SelectionStart, level);
        if (element is null)
        {
            announce($"No {(direction > 0 ? "next" : "previous")} {MarkdownOutline.Name(kind, level)}", true);
            return;
        }

        text.Select(element.Start, 0);
        text.ScrollToCaret();
        announce(element.Spoken, true);
    }

    /// <summary>
    /// The menu item text for a pair, "&amp;Heading", "Heading level &amp;1", "Lin&amp;k": the kind's name with NVDA's
    /// letter for it, which is the key's own letter or digit, as the mnemonic.
    /// </summary>
    public static string MenuLabel(TextJump jump)
    {
        var name = MarkdownOutline.Name(jump.Kind, jump.Level);
        var letter = BundleKeys.Text(jump.Next & Keys.KeyCode)[0];
        var at = name.IndexOf(letter, StringComparison.OrdinalIgnoreCase);
        return (char.ToUpperInvariant(name[0]) + name[1..]).Insert(at, "&");
    }
}

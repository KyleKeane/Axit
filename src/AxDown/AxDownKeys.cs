using Axit.Forms;

namespace AxDown;

/// <summary>
/// axDown's window-specific keys (SPEC.md AD-5; bundle AX-8). The editor shell's keys (new, open, save as, go to line,
/// word wrap, show in File Explorer) are <c>Axit.Forms.EditorKeys</c>, the same in every editor window; the Markdown
/// navigation keys of SPEC.md §9 are <c>Axit.Forms.MarkdownKeys</c>, shared with axPreview; <see cref="All"/> includes
/// both. The bundle-wide keys (F1, find, save, text size, the controls) are <c>Axit.Forms.BundleKeys</c>. The collision
/// test in <c>tests/Axit.Tests</c> fails when a key here equals one of those.
/// </summary>
public static class AxDownKeys
{
    /// <summary>Every key of the window with its name, the editor shell's first, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All = [..EditorKeys.All, ..MarkdownKeys.Named];

    /// <summary>The keys of SPEC.md §9 as pairs, one per kind, for the shell.</summary>
    public static IReadOnlyList<TextJump> Jumps => MarkdownKeys.Jumps;
}

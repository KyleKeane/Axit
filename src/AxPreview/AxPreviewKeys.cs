using Axit.Forms;

namespace AxPreview;

/// <summary>
/// axPreview's window-specific keys (SPEC.md AP-3.2; bundle AX-8). The viewer's keys of the shell (new window, open,
/// reload, edit in the editor, show in File Explorer, go to line, word wrap, the line number) are
/// <c>Axit.Forms.EditorKeys.Viewer</c>; the navigation letters are axDown's (docs/axdown/SPEC.md §9),
/// <c>Axit.Forms.MarkdownKeys</c>, so that a file read here and edited there is under the same keys (G2);
/// <see cref="All"/> includes both. The bundle-wide keys (F1, find, Ctrl+S, text size, the controls) are
/// <c>Axit.Forms.BundleKeys</c>.
/// </summary>
public static class AxPreviewKeys
{
    /// <summary>Every key of the window with its name, the shell's viewer keys first, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All = [..EditorKeys.Viewer, ..MarkdownKeys.Named];

    /// <summary>The navigation keys as pairs, one per kind, for the shell.</summary>
    public static IReadOnlyList<TextJump> Jumps => MarkdownKeys.Jumps;
}

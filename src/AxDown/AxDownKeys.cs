namespace AxDown;

/// <summary>
/// AxDown's window-specific keys (SPEC.md AD-5; bundle AX-8). A key is changed here and nowhere else: the menu items,
/// the key handling and the F1 text read this table. The bundle-wide keys (F1, find, save, text size, the controls)
/// are <c>Axit.Forms.BundleKeys</c>; the collision test in <c>tests/Axit.Tests</c> fails when a key here equals one of
/// those. The navigation keys of SPEC.md §9 (Ctrl+H and the rest of NVDA's letters) are added here when they are built.
/// </summary>
public static class AxDownKeys
{
    public const Keys NewDocument = Keys.Control | Keys.N;
    public const Keys Open = Keys.Control | Keys.O;
    public const Keys SaveAs = Keys.Control | Keys.Shift | Keys.S;
    public const Keys GoToLine = Keys.Control | Keys.G;
    public const Keys WordWrap = Keys.Control | Keys.Shift | Keys.W;

    /// <summary>Every key above with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("New document", NewDocument),
        ("Open", Open),
        ("Save as", SaveAs),
        ("Go to line", GoToLine),
        ("Word wrap", WordWrap),
    ];
}

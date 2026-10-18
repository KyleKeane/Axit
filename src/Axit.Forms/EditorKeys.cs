namespace Axit.Forms;

/// <summary>
/// The editor shell's keys (docs/axit/SPEC.md AX-8.3; docs/axdown/SPEC.md AD-5), the same in every text editor window
/// of the bundle: each editor app's own table includes <see cref="All"/>, so that one table per window still holds
/// every key and the collision test sees them. Handled in <see cref="TextEditorForm"/> and nowhere else.
/// </summary>
public static class EditorKeys
{
    public const Keys NewDocument = Keys.Control | Keys.N;
    public const Keys Open = Keys.Control | Keys.O;
    public const Keys SaveAs = Keys.Control | Keys.Shift | Keys.S;
    public const Keys GoToLine = Keys.Control | Keys.G;
    public const Keys WordWrap = Keys.Control | Keys.Shift | Keys.W;

    /// <summary>File → Show current file in → File Explorer (AD-4.1), the key AxClaude uses for its folder.</summary>
    public const Keys ShowInExplorer = Keys.Control | Keys.W;

    /// <summary>Every key above with its name, for the editor apps' tables, the collision test and the F1 texts.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("New document", NewDocument),
        ("Open", Open),
        ("Save as", SaveAs),
        ("Show the file in File Explorer", ShowInExplorer),
        ("Go to line", GoToLine),
        ("Word wrap", WordWrap),
    ];
}

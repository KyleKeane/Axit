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

    /// <summary>File → Show current file in → File Explorer (AD-4.1), the key axClaude uses for its folder.</summary>
    public const Keys ShowInExplorer = Keys.Control | Keys.W;

    /// <summary>Edit → Undo and Redo over the window's own history (AD-2.5); Ctrl+Shift+Z is Redo as well, as in most editors.</summary>
    public const Keys Undo = Keys.Control | Keys.Z;
    public const Keys Redo = Keys.Control | Keys.Y;
    public const Keys RedoAlso = Keys.Control | Keys.Shift | Keys.Z;

    /// <summary>Every key above with its name, for the editor apps' tables, the collision test and the F1 texts.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("New document", NewDocument),
        ("Open", Open),
        ("Save as", SaveAs),
        ("Show the file in File Explorer", ShowInExplorer),
        ("Undo", Undo),
        ("Redo", Redo),
        ("Redo (also)", RedoAlso),
        ("Go to line", GoToLine),
        ("Word wrap", WordWrap),
    ];

    // The viewer's keys (a read-only editor app, axPreview, docs/axpreview/SPEC.md AP-3.2): the shell handles them in
    // read-only mode only, and the viewer's table includes Viewer instead of All.

    /// <summary>File → Reload: the file read again, the caret kept (AP-2.4).</summary>
    public const Keys Reload = Keys.F5;

    /// <summary>File → Edit in axDown or axJSON: the file in its editor, a new window (AP-1.4).</summary>
    public const Keys Edit = Keys.Control | Keys.E;

    /// <summary>In the text: the line said, "Line 12 of 40" (AP-2.3); a plain letter, which a read-only text has no use for.</summary>
    public const Keys LineNumber = Keys.L;

    /// <summary>In the Details list: the line's value copied, without its name (AP-6.3); the text's own Ctrl+C elsewhere.</summary>
    public const Keys CopyDetail = Keys.Control | Keys.C;

    /// <summary>In the Details list: Enter on the Folder line opens the folder in axFile with the file selected (AP-6.3).</summary>
    public const Keys OpenDetail = Keys.Return;

    /// <summary>The viewer's keys with their names: the shell's keys that apply without editing (Ctrl+N is a new window there), and its own.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> Viewer =
    [
        ("New window", NewDocument),
        ("Open", Open),
        ("Reload", Reload),
        ("Edit in the editor", Edit),
        ("Show the file in File Explorer", ShowInExplorer),
        ("Go to line", GoToLine),
        ("Word wrap", WordWrap),
        ("Line number", LineNumber),
        ("Copy a detail's value", CopyDetail),
        ("Open the folder of the Folder detail", OpenDetail),
    ];
}

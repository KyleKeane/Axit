namespace AxFile;

/// <summary>
/// AxFile's window-specific keys (SPEC.md AF-6.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the key switch in <c>FileForm.ProcessWindowKey</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the control keys, find, Ctrl+S, text size) are <c>Axit.Forms.BundleKeys</c>. Enter, Backspace, Space, F2,
/// Delete, Shift+F10 and the Applications key act in the list only; in a notice's field they are the field's own.
/// </summary>
public static class AxFileKeys
{
    /// <summary>In the list: Enter opens the entry (a folder in the list, a file in its app, a media file in the player).</summary>
    public const Keys Open = Keys.Return;

    /// <summary>In the list: the parent folder, the folder just left selected; Alt+Up the same from anywhere in the window.</summary>
    public const Keys Up = Keys.Back;

    public const Keys UpAlso = Keys.Alt | Keys.Up;
    public const Keys Refresh = Keys.F5;
    public const Keys OpenFolder = Keys.Control | Keys.O;
    public const Keys GoToFolder = Keys.Control | Keys.G;
    public const Keys ShowInExplorer = Keys.Control | Keys.W;
    public const Keys ShowItemInExplorer = Keys.Control | Keys.Shift | Keys.W;
    public const Keys OpenInEditor = Keys.Control | Keys.E;
    public const Keys Information = Keys.Control | Keys.I;
    public const Keys NewFolder = Keys.Control | Keys.Shift | Keys.N;

    /// <summary>In the list: the Rename notice.</summary>
    public const Keys Rename = Keys.F2;

    /// <summary>In the list: the Delete question.</summary>
    public const Keys Delete = Keys.Delete;

    public const Keys CopyPath = Keys.Control | Keys.Shift | Keys.C;

    /// <summary>In the list: the Item menu on the entry, as the list's own context menu (AF-4.6); the list shows it itself, so the key switch has no case for it.</summary>
    public const Keys ContextMenu = Keys.Shift | Keys.F10;

    /// <summary>In the list: the same, from the Applications key.</summary>
    public const Keys ContextMenuAlso = Keys.Apps;

    /// <summary>The Windows Properties sheet of the entry, File Explorer's key for it (AF-4.7).</summary>
    public const Keys Properties = Keys.Alt | Keys.Return;

    /// <summary>In the list: the player pauses or resumes.</summary>
    public const Keys PauseResume = Keys.Space;

    public const Keys Forward = Keys.Control | Keys.Right;
    public const Keys Back = Keys.Control | Keys.Left;
    public const Keys NextFile = Keys.Control | Keys.Shift | Keys.Right;
    public const Keys PreviousFile = Keys.Control | Keys.Shift | Keys.Left;
    public const Keys GoToTime = Keys.Control | Keys.T;
    public const Keys Faster = Keys.Control | Keys.Shift | Keys.OemPeriod;
    public const Keys Slower = Keys.Control | Keys.Shift | Keys.Oemcomma;
    public const Keys WhatIsPlaying = Keys.Control | Keys.Q;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Open (in the list)", Open),
        ("Up one level (in the list)", Up),
        ("Up one level", UpAlso),
        ("Refresh", Refresh),
        ("Open folder", OpenFolder),
        ("Go to folder", GoToFolder),
        ("Show the folder in File Explorer", ShowInExplorer),
        ("Show the entry in File Explorer", ShowItemInExplorer),
        ("Open the file in its editor", OpenInEditor),
        ("Information about the entry", Information),
        ("New folder", NewFolder),
        ("Rename (in the list)", Rename),
        ("Delete (in the list)", Delete),
        ("Copy path", CopyPath),
        ("Item menu (in the list)", ContextMenu),
        ("Item menu (in the list)", ContextMenuAlso),
        ("Properties", Properties),
        ("Pause or resume (in the list)", PauseResume),
        ("Forward", Forward),
        ("Back", Back),
        ("Next file", NextFile),
        ("Previous file", PreviousFile),
        ("Go to time", GoToTime),
        ("Faster", Faster),
        ("Slower", Slower),
        ("What is playing", WhatIsPlaying),
    ];
}

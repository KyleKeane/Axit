namespace AxFile;

/// <summary>
/// AxFile's window-specific keys (SPEC.md AF-6.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the key switch in <c>FileForm.ProcessWindowKey</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the control keys, find, Ctrl+S, text size) are <c>Axit.Forms.BundleKeys</c>. Enter, Backspace, Space, F2,
/// Delete, Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+A, Ctrl+Shift+A, Shift+F10 and the Applications key act in the list only; in
/// a notice's field or the rename box they are the field's own.
/// </summary>
public static class AxFileKeys
{
    /// <summary>In the list: Enter opens the entry (a folder in the list, a file in its app, a media file in the player).</summary>
    public const Keys Open = Keys.Return;

    /// <summary>In the list: the parent folder, the folder just left selected; Alt+Up the same from anywhere in the window.</summary>
    public const Keys Up = Keys.Back;

    public const Keys UpAlso = Keys.Alt | Keys.Up;

    /// <summary>A new AxFile window on the folder shown, File Explorer's key for it (AF-3.3).</summary>
    public const Keys NewWindow = Keys.Control | Keys.N;

    public const Keys Refresh = Keys.F5;

    /// <summary>The folder shown added to Favorites, or removed from them (AF-3.9); the browsers' bookmark key.</summary>
    public const Keys Favorite = Keys.Control | Keys.D;

    /// <summary>The Filter notice: the list narrowed to the names that contain a text (AF-3.8).</summary>
    public const Keys Filter = Keys.Control | Keys.Shift | Keys.F;

    /// <summary>In the list: the filter cleared; nothing while there is none.</summary>
    public const Keys ClearFilter = Keys.Escape;
    public const Keys OpenFolder = Keys.Control | Keys.O;
    public const Keys GoToFolder = Keys.Control | Keys.G;
    public const Keys ShowInExplorer = Keys.Control | Keys.W;
    public const Keys ShowItemInExplorer = Keys.Control | Keys.Shift | Keys.W;
    public const Keys OpenInEditor = Keys.Control | Keys.E;
    public const Keys Information = Keys.Control | Keys.I;
    public const Keys NewFolder = Keys.Control | Keys.Shift | Keys.N;

    /// <summary>An empty text file made and the rename box opened on it (AF-4.1).</summary>
    public const Keys NewFile = Keys.Control | Keys.Shift | Keys.T;

    /// <summary>In the list: the rename in place, a box over the entry (AF-4.2).</summary>
    public const Keys Rename = Keys.F2;

    /// <summary>In the list: the Delete question.</summary>
    public const Keys Delete = Keys.Delete;

    public const Keys CopyPath = Keys.Control | Keys.Shift | Keys.C;

    /// <summary>In the list: the entry on the clipboard as a file, to move (AF-4.8).</summary>
    public const Keys Cut = Keys.Control | Keys.X;

    /// <summary>In the list: the entry on the clipboard as a file, to copy.</summary>
    public const Keys Copy = Keys.Control | Keys.C;

    /// <summary>In the list: the clipboard's files copied or moved into the folder shown.</summary>
    public const Keys Paste = Keys.Control | Keys.V;

    /// <summary>In the list: the entry selected, or deselected again (AF-3.7); the operations act on the selected entries.</summary>
    public const Keys Select = Keys.Space;

    /// <summary>In the list: every entry selected.</summary>
    public const Keys SelectAll = Keys.Control | Keys.A;

    /// <summary>In the list: no entry selected.</summary>
    public const Keys SelectNone = Keys.Control | Keys.Shift | Keys.A;

    /// <summary>In the list: the range from where it began to the next entry selected, as File Explorer's Shift+Down (AF-3.7).</summary>
    public const Keys SelectDown = Keys.Shift | Keys.Down;

    public const Keys SelectUp = Keys.Shift | Keys.Up;
    public const Keys SelectToStart = Keys.Shift | Keys.Home;
    public const Keys SelectToEnd = Keys.Shift | Keys.End;

    /// <summary>In the list: the Item menu on the entry, as the list's own context menu (AF-4.6); the list shows it itself, so the key switch has no case for it.</summary>
    public const Keys ContextMenu = Keys.Shift | Keys.F10;

    /// <summary>In the list: the same, from the Applications key.</summary>
    public const Keys ContextMenuAlso = Keys.Apps;

    /// <summary>The Windows Properties sheet of the entry, File Explorer's key for it (AF-4.7).</summary>
    public const Keys Properties = Keys.Alt | Keys.Return;

    /// <summary>The player pauses or resumes, from anywhere in the window (AF-5.4); Space is the selection's since 2026-10-21.</summary>
    public const Keys PauseResume = Keys.Control | Keys.P;

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
        ("New window", NewWindow),
        ("Refresh", Refresh),
        ("Add to or remove from Favorites", Favorite),
        ("Filter", Filter),
        ("Clear the filter (in the list)", ClearFilter),
        ("Open folder", OpenFolder),
        ("Go to folder", GoToFolder),
        ("Show the folder in File Explorer", ShowInExplorer),
        ("Show the entry in File Explorer", ShowItemInExplorer),
        ("Open the file in its editor", OpenInEditor),
        ("Information about the entry", Information),
        ("New folder", NewFolder),
        ("New text file", NewFile),
        ("Rename (in the list)", Rename),
        ("Delete (in the list)", Delete),
        ("Copy path", CopyPath),
        ("Cut (in the list)", Cut),
        ("Copy (in the list)", Copy),
        ("Paste (in the list)", Paste),
        ("Select or deselect (in the list)", Select),
        ("Select all (in the list)", SelectAll),
        ("Select none (in the list)", SelectNone),
        ("Select to the next entry (in the list)", SelectDown),
        ("Select to the previous entry (in the list)", SelectUp),
        ("Select to the first entry (in the list)", SelectToStart),
        ("Select to the last entry (in the list)", SelectToEnd),
        ("Item menu (in the list)", ContextMenu),
        ("Item menu (in the list)", ContextMenuAlso),
        ("Properties", Properties),
        ("Pause or resume", PauseResume),
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

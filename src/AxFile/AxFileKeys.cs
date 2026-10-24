namespace AxFile;

/// <summary>
/// axFile's window-specific keys (SPEC.md AF-6.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the key switch in <c>FileForm.ProcessWindowKey</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the control keys, find, Ctrl+S, text size) are <c>Axit.Forms.BundleKeys</c>. Enter, Backspace, Space, F2,
/// Delete, Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+A, Ctrl+Shift+A, Shift+F10 and the Applications key act in the folder list
/// only; in the Actions list Enter, Ctrl+Z, Ctrl+I and Escape act on its line; in a notice's field or the rename box
/// they are the field's own.
/// </summary>
public static class AxFileKeys
{
    /// <summary>In the folder list: Enter opens the entry (a folder in the list, a file in the program Windows has for it); in the Actions list: where the action left things.</summary>
    public const Keys Open = Keys.Return;

    /// <summary>In the folder list: the file read in axPreview (AF-3.2); a folder in a new axFile window, File Explorer's Shift+Enter.</summary>
    public const Keys Preview = Keys.Shift | Keys.Return;

    /// <summary>In the list: the parent folder, the folder just left selected; Alt+Up the same from anywhere in the window.</summary>
    public const Keys Up = Keys.Back;

    public const Keys UpAlso = Keys.Alt | Keys.Up;

    /// <summary>A new axFile window on the folder shown, File Explorer's key for it (AF-3.3).</summary>
    public const Keys NewWindow = Keys.Control | Keys.N;

    public const Keys Refresh = Keys.F5;

    /// <summary>The folder shown added to Favorites, or removed from them (AF-3.9); the browsers' bookmark key.</summary>
    public const Keys Favorite = Keys.Control | Keys.D;

    /// <summary>The Filter notice: the list narrowed to the names that contain a text (AF-3.8).</summary>
    public const Keys Filter = Keys.Control | Keys.Shift | Keys.F;

    /// <summary>In the folder list: the selection cleared while entries are selected, otherwise the filter; nothing while there is neither. In the Actions list: back to the folder list.</summary>
    public const Keys Clear = Keys.Escape;
    public const Keys OpenFolder = Keys.Control | Keys.O;
    public const Keys GoToFolder = Keys.Control | Keys.G;

    /// <summary>Go to folder too, File Explorer's key for its address bar (AF-3.3; the Drives menu's mnemonic until 2026-10-22).</summary>
    public const Keys GoToFolderAlso = Keys.Alt | Keys.D;
    public const Keys ShowInExplorer = Keys.Control | Keys.W;
    public const Keys ShowItemInExplorer = Keys.Control | Keys.Shift | Keys.W;
    public const Keys OpenInEditor = Keys.Control | Keys.E;

    /// <summary>The entry's size and date; in the Actions list, the line's number and time.</summary>
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

    /// <summary>In the list: every entry from the nearest selected one to the current one selected (AF-3.7), one line in the Actions list.</summary>
    public const Keys SelectTo = Keys.Shift | Keys.Space;

    /// <summary>In the list: the range from where it began to the next entry selected, as File Explorer's Shift+Down (AF-3.7).</summary>
    public const Keys SelectDown = Keys.Shift | Keys.Down;

    public const Keys SelectUp = Keys.Shift | Keys.Up;
    public const Keys SelectToStart = Keys.Shift | Keys.Home;
    public const Keys SelectToEnd = Keys.Shift | Keys.End;

    /// <summary>In the list: the menu bar's Item menu on the entry, opened as Alt and I open it (AF-4.6, AF-D13).</summary>
    public const Keys ContextMenu = Keys.Shift | Keys.F10;

    /// <summary>In the list: the same, from the Applications key.</summary>
    public const Keys ContextMenuAlso = Keys.Apps;

    /// <summary>The Windows Properties sheet of the entry, File Explorer's key for it (AF-4.7).</summary>
    public const Keys Properties = Keys.Alt | Keys.Return;

    /// <summary>The newest operation of this window taken back (AF-4.14); in the Actions list, the line under the selection (AF-9.3).</summary>
    public const Keys Undo = Keys.Control | Keys.Z;

    /// <summary>In either list, at the first entry: the edge earcon (AF-3.6; the bundle's `AxitList` since 2026-10-24); the list's own key everywhere else. In the Go to folder field: the previous completion (AF-3.3).</summary>
    public const Keys FirstItem = Keys.Up;

    /// <summary>The Go to folder field's previous completion (AF-3.3): the same key as <see cref="FirstItem"/>, in the field.</summary>
    public const Keys CompletePrevious = FirstItem;

    public const Keys FirstItemAlso = Keys.Home;

    /// <summary>In either list, at the last entry: the edge earcon; the list's own key everywhere else. In the Go to folder field: the next completion (AF-3.3).</summary>
    public const Keys LastItem = Keys.Down;

    /// <summary>The Go to folder field's next completion (AF-3.3): the same key as <see cref="LastItem"/>, in the field.</summary>
    public const Keys CompleteNext = LastItem;

    /// <summary>In the folder list: into the folder or drive, as Enter, the edge earcon on a file (AF-3.1, AF-D12). In the Go to folder field, at the end of a completed folder: its first entry.</summary>
    public const Keys IntoFolder = Keys.Right;

    /// <summary>In the folder list: up one level, as Backspace, the edge earcon at This PC (AF-3.1, AF-D12).</summary>
    public const Keys OutOfFolder = Keys.Left;

    public const Keys LastItemAlso = Keys.End;

    /// <summary>In the folder list: axMedia on the folder, its media files from the first (AF-5).</summary>
    public const Keys PlayFolder = Keys.Control | Keys.Return;

    /// <summary>In the folder list: the entry, or the marked entries, to the end of axMedia's queue (AF-5).</summary>
    public const Keys AddToQueue = Keys.Control | Keys.Shift | Keys.Q;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Open (in the folder list); go to the action's place (in the Actions list)", Open),
        ("Preview in axPreview, or a folder in a new window (in the folder list)", Preview),
        ("Up one level (in the folder list)", Up),
        ("Up one level", UpAlso),
        ("New window", NewWindow),
        ("Refresh", Refresh),
        ("Add to or remove from Favorites", Favorite),
        ("Filter", Filter),
        ("Clear the selection, or else the filter (in the folder list); back to the folder list (in the Actions list)", Clear),
        ("Open folder", OpenFolder),
        ("Go to folder", GoToFolder),
        ("Go to folder, File Explorer's address bar key", GoToFolderAlso),
        ("Show the folder in File Explorer", ShowInExplorer),
        ("Show the entry in File Explorer", ShowItemInExplorer),
        ("Open the file in its editor", OpenInEditor),
        ("Information about the entry, or the action's number and time", Information),
        ("New folder", NewFolder),
        ("New text file", NewFile),
        ("Rename (in the folder list)", Rename),
        ("Delete (in the folder list)", Delete),
        ("Copy path", CopyPath),
        ("Cut (in the folder list)", Cut),
        ("Copy (in the folder list)", Copy),
        ("Paste (in the folder list)", Paste),
        ("Select or deselect (in the folder list)", Select),
        ("Select all (in the folder list)", SelectAll),
        ("Deselect all (in the folder list)", SelectNone),
        ("Select to here, from the nearest selected entry (in the folder list)", SelectTo),
        ("Select to the next entry (in the folder list)", SelectDown),
        ("Select to the previous entry (in the folder list)", SelectUp),
        ("Select to the first entry (in the folder list)", SelectToStart),
        ("Select to the last entry (in the folder list)", SelectToEnd),
        ("Item menu (in the folder list)", ContextMenu),
        ("Item menu (in the folder list)", ContextMenuAlso),
        ("Properties", Properties),
        ("Undo the newest operation, or the action under the selection (in the Actions list)", Undo),
        ("The edge earcon at the first entry (in a list); the previous completion (in the Go to folder field)", FirstItem),
        ("The edge earcon at the first entry (in a list)", FirstItemAlso),
        ("The edge earcon at the last entry (in a list); the next completion (in the Go to folder field)", LastItem),
        ("Into the folder (in the folder list); into the completed folder (in the Go to folder field)", IntoFolder),
        ("Up one level (in the folder list)", OutOfFolder),
        ("The edge earcon at the last entry (in a list)", LastItemAlso),
        ("Play the folder in axMedia (in the folder list)", PlayFolder),
        ("Add to axMedia's queue (in the folder list)", AddToQueue),
    ];
}

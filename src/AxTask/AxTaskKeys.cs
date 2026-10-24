namespace AxTask;

/// <summary>
/// axTask's window-specific keys (docs/axit/item-apps.md IA-3, SPEC.md TK-7; bundle AX-8). A key is changed here and
/// nowhere else: the menu items, the key switch in <c>TaskForm.ProcessWindowKey</c> and the F1 text read this table.
/// The bundle-wide keys (F1, the palette, find, Ctrl+S, text size) are <c>Axit.Forms.BundleKeys</c>. Some keys mean
/// one thing in the navigator and its like in the detail pane (Ctrl+N a new item or a new field).
/// </summary>
public static class AxTaskKeys
{
    /// <summary>Into a list (IA-3): Right only goes into lists; on an item it plays the edge sound.</summary>
    public const Keys Into = Keys.Right;

    public const Keys Up = Keys.Left;
    public const Keys UpAlso = Keys.Back;

    /// <summary>Open (IA-D2): a list opens, an item's fields take the focus; in the detail pane, change the field.</summary>
    public const Keys Open = Keys.Return;

    /// <summary>To the detail pane and back (IA-3).</summary>
    public const Keys OtherPane = Keys.Tab;

    public const Keys OtherPaneBack = Keys.Shift | Keys.Tab;

    /// <summary>In the detail pane: back to the navigator.</summary>
    public const Keys Back = Keys.Escape;

    /// <summary>Done or not done (TK-4.4): in the navigator on a task, in the detail pane on a check.</summary>
    public const Keys Check = Keys.Space;

    /// <summary>A new item in the navigator, a new field in the detail pane.</summary>
    public const Keys New = Keys.Control | Keys.N;

    public const Keys NewList = Keys.Control | Keys.Shift | Keys.N;
    public const Keys Rename = Keys.F2;

    /// <summary>Archive the item or list (IA-D4); in the detail pane, remove the field.</summary>
    public const Keys Delete = Keys.Delete;

    public const Keys MoveFieldUp = Keys.Alt | Keys.Up;
    public const Keys MoveFieldDown = Keys.Alt | Keys.Down;
    public const Keys Undo = Keys.Control | Keys.Z;

    /// <summary>Read in full: every field of the item, or the field's whole value.</summary>
    public const Keys ReadAll = Keys.Control | Keys.Q;

    public const Keys Refresh = Keys.F5;
    public const Keys OpenInAxJson = Keys.Control | Keys.J;
    public const Keys ShowInAxFile = Keys.Control | Keys.E;
    public const Keys HideDone = Keys.Control | Keys.Shift | Keys.D;
    public const Keys AllTasks = Keys.Control | Keys.D1;

    /// <summary>The pane's menu from the bar, as Alt and its letter open it (the window base's OpenBarMenu).</summary>
    public const Keys ItemMenu = Keys.Shift | Keys.F10;

    public const Keys ItemMenuAlso = Keys.Apps;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Into a list", Into),
        ("Up a level", Up),
        ("Up a level (Backspace)", UpAlso),
        ("Open, or change the field", Open),
        ("To the other pane", OtherPane),
        ("To the other pane, back", OtherPaneBack),
        ("Back to the navigator (in the detail pane)", Back),
        ("Done or not done", Check),
        ("New item, or new field", New),
        ("New list", NewList),
        ("Rename", Rename),
        ("Archive, or remove the field", Delete),
        ("Move the field up", MoveFieldUp),
        ("Move the field down", MoveFieldDown),
        ("Undo", Undo),
        ("Read in full", ReadAll),
        ("Refresh", Refresh),
        ("Open in axJSON", OpenInAxJson),
        ("Show in axFile", ShowInAxFile),
        ("Hide or show done tasks", HideDone),
        ("All tasks", AllTasks),
        ("Menu of the pane", ItemMenu),
        ("Menu of the pane (the Applications key)", ItemMenuAlso),
    ];
}

namespace AxList;

/// <summary>
/// axList's window-specific keys (SPEC.md LI-6.5; bundle AX-8). A key is changed here and nowhere else: the menu items,
/// the key switch in <c>ListForm.ProcessWindowKey</c> and the F1 text read this table. The bundle-wide keys (F1, the
/// palette, find, Ctrl+S, text size) are <c>Axit.Forms.BundleKeys</c>. Every key acts on the list box's view.
/// </summary>
public static class AxListKeys
{
    /// <summary>The three views (LI-2.2); the key names the view when it already shows (axClaude D26).</summary>
    public const Keys ViewLists = Keys.Control | Keys.D1;

    public const Keys ViewItems = Keys.Control | Keys.D2;
    public const Keys ViewTasks = Keys.Control | Keys.D3;

    /// <summary>Lists: open the list; Items: change the item's text; Tasks: the task in its list.</summary>
    public const Keys Act = Keys.Return;

    /// <summary>Check or uncheck a task (LI-4.2, LI-5.2).</summary>
    public const Keys Check = Keys.Space;

    /// <summary>Lists: a new list; Items: a new item (LI-3.2, LI-4.4).</summary>
    public const Keys New = Keys.Control | Keys.N;

    public const Keys Remove = Keys.Delete;
    public const Keys MoveUp = Keys.Alt | Keys.Up;
    public const Keys MoveDown = Keys.Alt | Keys.Down;
    public const Keys Indent = Keys.Alt | Keys.Right;
    public const Keys Outdent = Keys.Alt | Keys.Left;
    public const Keys MakeTask = Keys.Control | Keys.T;
    public const Keys HideDone = Keys.Control | Keys.Shift | Keys.D;
    public const Keys Undo = Keys.Control | Keys.Z;
    public const Keys CopyText = Keys.Control | Keys.C;
    public const Keys ShowInAxFile = Keys.Control | Keys.E;
    public const Keys Refresh = Keys.F5;

    /// <summary>Items: back to the Lists view (LI-2.5).</summary>
    public const Keys Back = Keys.Escape;

    public const Keys BackAlso = Keys.Back;

    /// <summary>The views in turn, as axProcess's (the window has one control; the user's ask, 2026-10-24).</summary>
    public const Keys NextView = Keys.Tab;

    public const Keys PreviousView = Keys.Shift | Keys.Tab;

    /// <summary>In the list: the view's menu from the bar, List, Item or Task (the window base's OpenBarMenu).</summary>
    public const Keys ItemMenu = Keys.Shift | Keys.F10;

    public const Keys ItemMenuAlso = Keys.Apps;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Lists view", ViewLists),
        ("Items view", ViewItems),
        ("Tasks view", ViewTasks),
        ("Open, change or go to (in the list)", Act),
        ("Check or uncheck the task", Check),
        ("New list or new item", New),
        ("Remove the item", Remove),
        ("Move the item up", MoveUp),
        ("Move the item down", MoveDown),
        ("Indent the item", Indent),
        ("Outdent the item", Outdent),
        ("Make a task or not", MakeTask),
        ("Hide done tasks", HideDone),
        ("Undo", Undo),
        ("Copy the text", CopyText),
        ("Show in axFile", ShowInAxFile),
        ("Refresh", Refresh),
        ("Back to the lists", Back),
        ("Back to the lists (Backspace)", BackAlso),
        ("Next view", NextView),
        ("Previous view", PreviousView),
        ("Item menu (in the list)", ItemMenu),
        ("Item menu (in the list, the Applications key)", ItemMenuAlso),
    ];
}

using Axit.Forms;

namespace AxTask;

/// <summary>
/// axTask's window-specific keys (SPEC.md TK-7; bundle AX-8): the item apps' shared keys (<see cref="ItemKeys"/>,
/// docs/axit/item-apps.md IA-3) and axTask's own. A key is changed here or in <see cref="ItemKeys"/> and nowhere else:
/// the menu items, the key switches (<c>ItemForm.ProcessWindowKey</c>, <c>TaskForm.ProcessItemKey</c>) and the F1
/// text read these tables. The bundle-wide keys (F1, the palette, find, Ctrl+S, text size) are <c>BundleKeys</c>.
/// </summary>
public static class AxTaskKeys
{
    public const Keys OpenInAxJson = Keys.Control | Keys.J;
    public const Keys ShowInAxFile = Keys.Control | Keys.E;
    public const Keys HideDone = Keys.Control | Keys.Shift | Keys.D;
    public const Keys AllTasks = Keys.Control | Keys.D1;

    /// <summary>On a list among the sections: its section chosen (TK-3a.3).</summary>
    public const Keys MoveToSection = Keys.Control | Keys.M;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        .. ItemKeys.All,
        ("Open in axJSON", OpenInAxJson),
        ("Show in axFile", ShowInAxFile),
        ("Hide or show done tasks", HideDone),
        ("All tasks", AllTasks),
        ("Move the list to a section", MoveToSection),
    ];
}

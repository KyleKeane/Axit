namespace AxProcess;

/// <summary>
/// axProcess's window-specific keys (SPEC.md PR-6.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the key switch in <c>ProcessForm.ProcessWindowKey</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the palette, find, Ctrl+S, text size) are <c>Axit.Forms.BundleKeys</c>. Enter and Delete act in the list.
/// </summary>
public static class AxProcessKeys
{
    /// <summary>The three views of the list (PR-2.2); the key names the view when it already shows (axClaude D26).</summary>
    public const Keys ViewProcesses = Keys.Control | Keys.D1;

    public const Keys ViewServices = Keys.Control | Keys.D2;
    public const Keys ViewStartup = Keys.Control | Keys.D3;

    public const Keys Refresh = Keys.F5;

    /// <summary>In the list: the item's details with what can be done to it.</summary>
    public const Keys Act = Keys.Return;

    /// <summary>In the list: end the process, after a question (PR-3.3).</summary>
    public const Keys End = Keys.Delete;

    public const Keys ShowProgram = Keys.Control | Keys.E;
    public const Keys CopyLine = Keys.Control | Keys.C;
    public const Keys Summary = Keys.Control | Keys.Q;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Processes view", ViewProcesses),
        ("Services view", ViewServices),
        ("Startup view", ViewStartup),
        ("Refresh", Refresh),
        ("Details (in the list)", Act),
        ("End the process (in the list)", End),
        ("Show the program in axFile", ShowProgram),
        ("Copy the line", CopyLine),
        ("Summary", Summary),
    ];
}

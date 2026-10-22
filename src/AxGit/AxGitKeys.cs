namespace AxGit;

/// <summary>
/// AxGit's window-specific keys (SPEC.md AG-6.2; bundle AX-8). A key is changed here and nowhere else: the menu
/// items, the key switch in <c>GitForm.ProcessCmdKey</c> and the F1 text read this table. The bundle-wide keys
/// (F1, the control keys, find, Ctrl+S, text size, the edit keys of the details) are <c>Axit.Forms.BundleKeys</c>.
/// Space, Enter and Delete act in the list only; in the details they are the control's own.
/// </summary>
public static class AxGitKeys
{
    public const Keys OpenRepository = Keys.Control | Keys.O;
    public const Keys Refresh = Keys.F5;
    public const Keys ShowInExplorer = Keys.Control | Keys.W;

    /// <summary>The three views of the list (AG-2.2); the key names the view when it already shows (AxClaude D26).</summary>
    public const Keys ViewChanges = Keys.Control | Keys.D1;

    public const Keys ViewLog = Keys.Control | Keys.D2;
    public const Keys ViewBranches = Keys.Control | Keys.D3;

    /// <summary>In the list: Space stages or unstages, Enter acts on the item (diff, commit, switch), Delete discards.</summary>
    public const Keys StageToggle = Keys.Space;

    public const Keys Act = Keys.Return;
    public const Keys Discard = Keys.Delete;

    public const Keys StageAll = Keys.Control | Keys.Shift | Keys.A;
    public const Keys UnstageAll = Keys.Control | Keys.Shift | Keys.U;
    public const Keys OpenInEditor = Keys.Control | Keys.E;
    public const Keys Commit = Keys.Control | Keys.Return;
    public const Keys NewBranch = Keys.Control | Keys.N;
    public const Keys Fetch = Keys.Control | Keys.Shift | Keys.F;
    public const Keys Pull = Keys.Control | Keys.Shift | Keys.L;
    public const Keys Push = Keys.Control | Keys.Shift | Keys.H;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        ("Open repository", OpenRepository),
        ("Refresh", Refresh),
        ("Show the repository in File Explorer", ShowInExplorer),
        ("Changes view", ViewChanges),
        ("Log view", ViewLog),
        ("Branches view", ViewBranches),
        ("Stage or unstage (in the list)", StageToggle),
        ("Act on the item (in the list)", Act),
        ("Discard changes (in the list)", Discard),
        ("Stage all", StageAll),
        ("Unstage all", UnstageAll),
        ("Open the file in its editor", OpenInEditor),
        ("Commit", Commit),
        ("New branch", NewBranch),
        ("Fetch", Fetch),
        ("Pull", Pull),
        ("Push", Push),
    ];
}

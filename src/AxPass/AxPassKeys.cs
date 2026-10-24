using Axit.Forms;

namespace AxPass;

/// <summary>
/// axPass's window-specific keys (SPEC.md PW-7; bundle AX-8): the item apps' shared keys (<see cref="ItemKeys"/>,
/// docs/axit/item-apps.md IA-3) and axPass's own, KeePass's usual ones where it has them (Ctrl+B, Ctrl+C, Ctrl+U,
/// Ctrl+L). A key is changed here or in <see cref="ItemKeys"/> and nowhere else.
/// </summary>
public static class AxPassKeys
{
    public const Keys CopyUserName = Keys.Control | Keys.B;
    public const Keys CopyPassword = Keys.Control | Keys.C;
    public const Keys OpenUrl = Keys.Control | Keys.U;
    public const Keys Lock = Keys.Control | Keys.L;
    public const Keys OpenDatabase = Keys.Control | Keys.O;

    /// <summary>On an entry: move it to another group (PW-6.5), as axTask's Ctrl+M moves a list to a section.</summary>
    public const Keys MoveEntry = Keys.Control | Keys.M;

    /// <summary>Every key of the window with its name, for the collision test and the F1 text.</summary>
    public static readonly IReadOnlyList<(string Name, Keys Key)> All =
    [
        .. ItemKeys.All,
        ("Copy the user name", CopyUserName),
        ("Copy the password", CopyPassword),
        ("Open the URL", OpenUrl),
        ("Lock", Lock),
        ("Open a database", OpenDatabase),
        ("Move the entry to a group", MoveEntry),
    ];
}

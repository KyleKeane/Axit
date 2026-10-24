namespace Axit.Forms;

/// <summary>What a command does beyond reading its window, for the control of AI clients (docs/axit/mcp.md MC-3.1).</summary>
public enum CommandMark
{
    /// <summary>Says or shows something; changes nothing.</summary>
    Read,

    /// <summary>Opens a window or a file, moves in the window.</summary>
    Open,

    /// <summary>Changes the window's own state or the file it holds.</summary>
    Change,

    /// <summary>Acts outside the window: deletes, moves, runs, sends, installs. An AI client never runs one on its own word.</summary>
    Act,
}

/// <summary>
/// One command of a window (docs/axit/app-kit.md AK-2): its menu label with its letter, its key from the window's key
/// table (<see cref="Keys.None"/> for a command that has none), what runs, and where the key acts (<see cref="When"/>,
/// null for anywhere in the window). The window makes its menu item and its key from this one record
/// (<see cref="AxitForm.Add"/>, <see cref="AxitForm.AddKey"/>), so that the menu, the key and the command palette
/// cannot disagree, and the planned MCP access lists the commands by their <see cref="Mark"/>.
/// </summary>
public sealed record Command(string Label, Keys Key, Action Run)
{
    /// <summary>Whether the key acts now (the list has the focus, the field has it); the menu item always runs.</summary>
    public Func<bool>? When { get; init; }

    /// <summary>What the command does beyond reading, for MCP; a change by default.</summary>
    public CommandMark Mark { get; init; } = CommandMark.Change;
}

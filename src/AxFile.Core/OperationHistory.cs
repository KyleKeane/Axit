namespace AxFile.Core;

/// <summary>The kinds of operation a window can take back (SPEC.md AF-4.14).</summary>
public enum OperationKind
{
    Rename,
    NewFolder,
    NewFile,
    Move,
    Copy,
    Compress,
    Extract,
    Delete,
}

/// <summary>
/// One operation the window made, with what its undo needs (SPEC.md AF-4.14): the paths before it and after it. A
/// rename has the old path and the new; a new folder, a new file, a compress and an extract only what they made; a
/// move and a copy the sources and where each landed (a move into a new folder names it, to remove when it is empty
/// again); a delete the paths that went to the Recycle Bin.
/// </summary>
public sealed record Operation(OperationKind Kind, IReadOnlyList<string> Before, IReadOnlyList<string> After, string? MadeFolder = null)
{
    /// <summary>The operation's name in the menu item, "Undo rename".</summary>
    public string Label => Kind switch
    {
        OperationKind.Rename => "rename",
        OperationKind.NewFolder => "new folder",
        OperationKind.NewFile => "new text file",
        OperationKind.Move => "move",
        OperationKind.Copy => "paste",
        OperationKind.Compress => "compress",
        OperationKind.Extract => "extract",
        _ => "delete",
    };

    /// <summary>Whether the undo puts entries back where they were (<see cref="Before"/>), rather than removing what the operation made (<see cref="After"/>).</summary>
    private bool PutsBack => Kind is OperationKind.Rename or OperationKind.Move or OperationKind.Delete;

    /// <summary>The paths the undo lands on: where the entries go back to, or what is removed.</summary>
    private IReadOnlyList<string> Paths => PutsBack ? Before : After;

    /// <summary>The folder the undo lands in, to show afterwards.</summary>
    public string Folder => Path.GetDirectoryName(Paths[0]) ?? Paths[0];

    /// <summary>The entry to select afterwards: the one put back; null when the undo removed what it made.</summary>
    public string? Select => PutsBack ? Path.GetFileName(Before[0]) : null;

    /// <summary>"notes.md" or "3 items".</summary>
    public string What => Paths.Count == 1 ? Path.GetFileName(Paths[0]) : $"{Paths.Count} items";

    /// <summary>The announcement once the undo is done.</summary>
    public string DoneText => Kind switch
    {
        OperationKind.Rename => $"Undone: renamed back to {Path.GetFileName(Before[0])}",
        OperationKind.NewFolder or OperationKind.NewFile => $"Undone: {What} removed",
        OperationKind.Move => "Undone: moved back",
        OperationKind.Delete => $"Undone: {What} restored",
        _ => $"Undone: {What} sent to the Recycle Bin",
    };
}

/// <summary>The window's own history of operations (SPEC.md AF-4.14): the last twenty, newest last, gone with the window; no redo.</summary>
public sealed class OperationHistory
{
    public const int Limit = 20;

    private readonly List<Operation> _operations = [];

    public int Count => _operations.Count;

    public void Push(Operation operation)
    {
        _operations.Add(operation);
        while (_operations.Count > Limit)
        {
            _operations.RemoveAt(0);
        }
    }

    /// <summary>The operation Undo would take back; null with none.</summary>
    public Operation? Peek() => _operations.Count > 0 ? _operations[^1] : null;

    /// <summary>The last operation, taken off the history for the window to undo; null with none.</summary>
    public Operation? Pop()
    {
        if (_operations.Count == 0)
        {
            return null;
        }

        var last = _operations[^1];
        _operations.RemoveAt(_operations.Count - 1);
        return last;
    }

    public void Clear() => _operations.Clear();

    /// <summary>The Folder menu's item, named after what it would undo: "Un&amp;do rename", or "Un&amp;do" with nothing to undo.</summary>
    public string MenuText => Peek() is { } last ? $"Un&do {last.Label}" : "Un&do";
}

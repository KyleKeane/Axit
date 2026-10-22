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
/// again); a delete the paths that went to the Recycle Bin. The Actions list (<see cref="ActionLog"/>) holds the
/// operations the window can still take back.
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

    /// <summary>Where the operation left things (the Actions list's Enter, AF-9.3): the folder of what it made or moved into, or of what it deleted.</summary>
    public string ResultFolder => Kind == OperationKind.Delete
        ? Path.GetDirectoryName(Before[0]) ?? Before[0]
        : Path.GetDirectoryName(MadeFolder ?? After[0]) ?? After[0];

    /// <summary>The entry to select there: what it made, the folder it moved into, or the first entry it copied or moved; null for a delete.</summary>
    public string? Result => Kind == OperationKind.Delete ? null : Path.GetFileName(MadeFolder ?? After[0]);

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


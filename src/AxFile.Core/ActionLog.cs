namespace AxFile.Core;

/// <summary>Where a line of the Actions list is (SPEC.md AF-9.2): running, done, failed, undone, or refused when undo was asked.</summary>
public enum ActionState
{
    Running,
    Done,
    Failed,
    Undone,
    NotUndone,
}

/// <summary>
/// One line of the Actions list (SPEC.md AF-9.2): what the operation did ("Renamed notes.md to todo.md"), its state,
/// the <see cref="Operation"/> for Undo while it can still be taken back, where it left things (the folder and the
/// entry to select, for Enter on the line, AF-9.3) and when it ran. The window keeps the object it was given back and
/// moves it on through the log; a line that fell off the end of the log is left alone.
/// </summary>
public sealed class ActionEntry
{
    internal ActionEntry(string text, string folder, DateTime time)
    {
        Text = text;
        Folder = folder;
        Time = time;
    }

    /// <summary>What the operation did, or is doing ("Copying 3 items", "Compressing, 40 of 120 files"), or "Failed: <reason>".</summary>
    public string Text { get; internal set; }

    public ActionState State { get; internal set; } = ActionState.Running;

    /// <summary>The operation Undo takes back; null while it runs, after it failed, and once it is older than the last twenty undoable ones.</summary>
    public Operation? Operation { get; internal set; }

    /// <summary>The folder the action left its result in; while it runs, the folder it runs in.</summary>
    public string Folder { get; internal set; }

    /// <summary>The entry to select there: what the action made or moved; null for a delete and while it runs.</summary>
    public string? Select { get; internal set; }

    public DateTime Time { get; internal set; }

    /// <summary>Why the undo was refused (<see cref="ActionState.NotUndone"/>), "notes.md has changed".</summary>
    public string? Reason { get; internal set; }

    /// <summary>The list's line: the text, with ", undone" or ", not undone: <reason>" after it (AF-9.2).</summary>
    public string Line => State switch
    {
        ActionState.Undone => Text + ", undone",
        ActionState.NotUndone => Text + ", not undone: " + Reason,
        _ => Text,
    };

    /// <summary>Whether Ctrl+Z on the line, or anywhere when it is the newest, can take it back.</summary>
    public bool CanUndo => State == ActionState.Done && Operation is not null;

    /// <summary>Where Enter on the line goes (AF-9.3): the folder and the entry the action left, or, once undone, where the undo put things back.</summary>
    public (string Folder, string? Select) Location =>
        State == ActionState.Undone && Operation is { } operation ? (operation.Folder, operation.Select) : (Folder, Select);
}

/// <summary>
/// The Actions list (SPEC.md AF-9.2, AF-9.3): every file operation of a window as one line, in time order, newest
/// last, the last fifty kept and the last twenty undoable, gone with the window. A running operation begins a line and
/// ends it with its result or its failure; one that ran at once adds its line done. It replaced <c>OperationHistory</c>
/// and the status lines of the queue pane on 2026-10-22 (docs/axit/axfile-split.md).
/// </summary>
public sealed class ActionLog
{
    public const int Limit = 50;
    public const int UndoLimit = 20;

    private readonly List<ActionEntry> _entries = [];
    private readonly Func<DateTime> _clock;

    public ActionLog()
        : this(() => DateTime.Now)
    {
    }

    /// <param name="clock">The time a line gets; the tests pass their own.</param>
    public ActionLog(Func<DateTime> clock)
    {
        _clock = clock;
    }

    public IReadOnlyList<ActionEntry> Entries => _entries;

    public int Count => _entries.Count;

    /// <summary>The newest line, whatever its state; null while the log is empty.</summary>
    public ActionEntry? Latest => _entries.Count > 0 ? _entries[^1] : null;

    /// <summary>A running operation's line, "Copying 3 items", in <paramref name="folder"/>; the entry to end or fail with.</summary>
    public ActionEntry Begin(string text, string folder)
    {
        var entry = new ActionEntry(text, folder, _clock());
        Append(entry);
        return entry;
    }

    /// <summary>A running line's text moved on, "Compressing, 40 of 120 files".</summary>
    public void Progress(ActionEntry entry, string text)
    {
        if (entry.State == ActionState.Running)
        {
            entry.Text = text;
        }
    }

    /// <summary>The operation ended: its line says what it did, and the operation, when there is one, is what Undo takes back and where Enter goes.</summary>
    public void End(ActionEntry entry, string text, Operation? operation)
    {
        entry.Text = text;
        entry.State = ActionState.Done;
        entry.Time = _clock();
        if (operation is not null)
        {
            entry.Operation = operation;
            entry.Folder = operation.ResultFolder;
            entry.Select = operation.Result;
        }

        TrimUndoable();
    }

    /// <summary>The operation failed: "Failed: <reason>", nothing to undo.</summary>
    public void Fail(ActionEntry entry, string reason)
    {
        entry.Text = "Failed: " + reason;
        entry.State = ActionState.Failed;
        entry.Time = _clock();
    }

    /// <summary>An operation that ran at once (a rename, a new folder): its line, done.</summary>
    public ActionEntry Add(string text, Operation operation)
    {
        var entry = new ActionEntry(text, operation.ResultFolder, _clock())
        {
            State = ActionState.Done,
            Operation = operation,
            Select = operation.Result,
        };
        Append(entry);
        TrimUndoable();
        return entry;
    }

    /// <summary>The newest line Undo can take back (Ctrl+Z anywhere, AF-9.3); null when none can.</summary>
    public ActionEntry? Newest()
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].CanUndo)
            {
                return _entries[i];
            }
        }

        return null;
    }

    /// <summary>The line taken back: it reads "…, undone" and cannot be undone again.</summary>
    public void Undone(ActionEntry entry) => entry.State = ActionState.Undone;

    /// <summary>The undo refused (the entry has changed, the place is taken): the line says so and the operation is dropped, so that the next Ctrl+Z moves on.</summary>
    public void NotUndone(ActionEntry entry, string reason)
    {
        entry.State = ActionState.NotUndone;
        entry.Reason = reason;
    }

    /// <summary>The list's lines, oldest first.</summary>
    public IReadOnlyList<string> Lines() => _entries.Select(entry => entry.Line).ToList();

    public int IndexOf(ActionEntry entry) => _entries.IndexOf(entry);

    /// <summary>Ctrl+I on a line: its number and time, "3 of 12, 21:04".</summary>
    public string Information(int index) => $"{index + 1} of {_entries.Count}, {_entries[index].Time:HH:mm}";

    /// <summary>The Folder menu's item, named after what Undo would take back: "Un&amp;do rename", or "Un&amp;do" with nothing to undo.</summary>
    public string MenuText => Newest() is { Operation: { } operation } ? $"Un&do {operation.Label}" : "Un&do";

    private void Append(ActionEntry entry)
    {
        _entries.Add(entry);
        while (_entries.Count > Limit)
        {
            _entries.RemoveAt(0);
        }
    }

    /// <summary>Beyond twenty undoable lines the oldest keeps its text and loses its operation (AF-4.14's limit).</summary>
    private void TrimUndoable()
    {
        var undoable = _entries.Count(entry => entry.CanUndo);
        for (var i = 0; i < _entries.Count && undoable > UndoLimit; i++)
        {
            if (_entries[i].CanUndo)
            {
                _entries[i].Operation = null;
                undoable--;
            }
        }
    }
}

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

/// <summary>What a line records (SPEC.md AF-9.2): a file operation, or a change of the marks (AF-3.7).</summary>
public enum ActionKind
{
    Operation,
    Selection,
}

/// <summary>The three sections of the Actions list (SPEC.md AF-9.2), in their order: the running operations, the operations, the selection.</summary>
public enum ActionSection
{
    Running,
    Operations,
    Selection,
}

/// <summary>
/// One line of the Actions list (SPEC.md AF-9.2): what the operation did ("Renamed notes.md to todo.md"), its state,
/// the <see cref="Operation"/> for Undo while it can still be taken back, where it left things (the folder and the
/// entry to select, for Enter on the line, AF-9.3) and when it ran; or a change of the marks ("Selected notes.md") with
/// the marks before it, which its undo puts back. The window keeps the object it was given back and moves it on
/// through the log; a line that fell off the end of the log is left alone.
/// </summary>
public sealed class ActionEntry
{
    internal ActionEntry(ActionKind kind, string text, string folder, DateTime time)
    {
        Kind = kind;
        Text = text;
        Folder = folder;
        Time = time;
    }

    public ActionKind Kind { get; }

    /// <summary>What the operation did, or is doing ("Copying 3 items", "Compressing, 40 of 120 files"), or "Failed: <reason>".</summary>
    public string Text { get; internal set; }

    public ActionState State { get; internal set; } = ActionState.Running;

    /// <summary>The operation Undo takes back; null while it runs, after it failed, and once it is older than the last twenty undoable ones.</summary>
    public Operation? Operation { get; internal set; }

    /// <summary>A selection line: the paths marked before it, which its undo puts back (AF-9.3); null on an operation's line.</summary>
    public IReadOnlyList<string>? Before { get; internal set; }

    /// <summary>The folder the action left its result in; while it runs, the folder it runs in.</summary>
    public string Folder { get; internal set; }

    /// <summary>The entry to select there: what the action made or moved, or marked; null for a delete and while it runs.</summary>
    public string? Select { get; set; }

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

    /// <summary>The section the line is in (AF-9.2): a change of the marks in Selection, a running operation in Running, every other operation in Operations.</summary>
    public ActionSection Section => Kind == ActionKind.Selection ? ActionSection.Selection
        : State == ActionState.Running ? ActionSection.Running
        : ActionSection.Operations;

    /// <summary>Whether Ctrl+Z on the line, or anywhere when it is the newest operation, can take it back.</summary>
    public bool CanUndo => State == ActionState.Done && (Operation is not null || Before is not null);

    /// <summary>Where Enter on the line goes (AF-9.3): the folder and the entry the action left, or, once undone, where the undo put things back.</summary>
    public (string Folder, string? Select) Location =>
        State == ActionState.Undone && Operation is { } operation ? (operation.Folder, operation.Select) : (Folder, Select);
}

/// <summary>
/// The Actions list (SPEC.md AF-9.2, AF-9.3): every file operation of a window and every change of its marks as one
/// line, shown in three sections under header lines, the running operations first, then the operations, then the
/// selection, each section in time order, newest last; the last fifty operations kept and the last twenty undoable,
/// the last twenty selection lines kept and gone with the folder; all gone with the window. A running operation
/// begins a line and ends it with its result or its failure; one that ran at once adds its line done. It replaced
/// <c>OperationHistory</c> and the status lines of the queue pane on 2026-10-22 (docs/axit/axfile-split.md).
/// </summary>
public sealed class ActionLog
{
    public const int Limit = 50;
    public const int SelectionLimit = 20;
    public const int UndoLimit = 20;

    private static readonly ActionSection[] Sections = [ActionSection.Running, ActionSection.Operations, ActionSection.Selection];

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

    /// <summary>A section's header line (AF-9.2).</summary>
    public static string Header(ActionSection section) => section switch
    {
        ActionSection.Running => "Running",
        ActionSection.Operations => "Operations",
        _ => "Selection",
    };

    /// <summary>"1 item", "5 items": a count in a line.</summary>
    public static string Items(int count) => count == 1 ? "1 item" : $"{count} items";

    /// <summary>Every line, in time order, whatever its section.</summary>
    public IReadOnlyList<ActionEntry> Entries => _entries;

    public int Count => _entries.Count;

    /// <summary>The newest line of any kind, which Tab lands on after it was added; null while the log is empty.</summary>
    public ActionEntry? Latest => _entries.Count > 0 ? _entries[^1] : null;

    /// <summary>The newest operation's line, whatever its state, for the status bar (AF-2.3); never a selection line.</summary>
    public ActionEntry? LatestOperation => _entries.LastOrDefault(entry => entry.Kind == ActionKind.Operation);

    /// <summary>A running operation's line, "Copying 3 items", in <paramref name="folder"/>; the entry to end or fail with.</summary>
    public ActionEntry Begin(string text, string folder)
    {
        var entry = new ActionEntry(ActionKind.Operation, text, folder, _clock());
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
        var entry = new ActionEntry(ActionKind.Operation, text, operation.ResultFolder, _clock())
        {
            State = ActionState.Done,
            Operation = operation,
            Select = operation.Result,
        };
        Append(entry);
        TrimUndoable();
        return entry;
    }

    /// <summary>A change of the marks (AF-3.7), "Selected notes.md", done at once, in <paramref name="folder"/> on <paramref name="select"/>, with the paths marked before it for its undo.</summary>
    public ActionEntry Mark(string text, string folder, string? select, IReadOnlyList<string> before)
    {
        var entry = new ActionEntry(ActionKind.Selection, text, folder, _clock())
        {
            State = ActionState.Done,
            Before = before,
            Select = select,
        };
        Append(entry);
        return entry;
    }

    /// <summary>A range's line moved on while the range grows or shrinks, "Selected 5 items" (AF-9.2); nothing once the line is undone.</summary>
    public void Rewrite(ActionEntry entry, string text)
    {
        if (entry.State == ActionState.Done)
        {
            entry.Text = text;
        }
    }

    /// <summary>Another folder shown: the marks are gone (AF-3.7), and their lines with them.</summary>
    public void DropSelection() => _entries.RemoveAll(entry => entry.Kind == ActionKind.Selection);

    /// <summary>The newest operation Undo can take back (Ctrl+Z in the folder list, AF-4.14), never a selection line; null when none can.</summary>
    public ActionEntry? Newest()
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].Kind == ActionKind.Operation && _entries[i].CanUndo)
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

    /// <summary>The list's lines (AF-9.2): each section that has lines, its header first, running first, then the operations, then the selection.</summary>
    public IReadOnlyList<string> Lines() => Rows().Select(row => row.Line).ToList();

    /// <summary>The entry on a line of the list; null on a header line and beyond the list.</summary>
    public ActionEntry? EntryAt(int index)
    {
        var rows = Rows();
        return index >= 0 && index < rows.Count ? rows[index].Entry : null;
    }

    /// <summary>An entry's line in the list; -1 once it has fallen off.</summary>
    public int IndexOf(ActionEntry entry) => Rows().FindIndex(row => row.Entry == entry);

    /// <summary>Ctrl+I on a line: its section, its number in it and its time, "Operations, 3 of 12, 21:04"; a header line, its name.</summary>
    public string Information(int index)
    {
        var row = Rows()[index];
        if (row.Entry is not { } entry)
        {
            return row.Line;
        }

        var section = _entries.Where(other => other.Section == entry.Section).ToList();
        return $"{Header(entry.Section)}, {section.IndexOf(entry) + 1} of {section.Count}, {entry.Time:HH:mm}";
    }

    /// <summary>The Folder menu's item, named after what Undo would take back: "Un&amp;do rename", or "Un&amp;do" with nothing to undo.</summary>
    public string MenuText => Newest() is { Operation: { } operation } ? $"Un&do {operation.Label}" : "Un&do";

    private List<(string Line, ActionEntry? Entry)> Rows()
    {
        var rows = new List<(string, ActionEntry?)>();
        foreach (var section in Sections)
        {
            var lines = _entries.Where(entry => entry.Section == section).ToList();
            if (lines.Count == 0)
            {
                continue;
            }

            rows.Add((Header(section), null));
            rows.AddRange(lines.Select(entry => (entry.Line, (ActionEntry?)entry)));
        }

        return rows;
    }

    private void Append(ActionEntry entry)
    {
        _entries.Add(entry);
        Trim(ActionKind.Operation, Limit);
        Trim(ActionKind.Selection, SelectionLimit);
    }

    /// <summary>Beyond a kind's limit its oldest lines fall off.</summary>
    private void Trim(ActionKind kind, int limit)
    {
        var count = _entries.Count(entry => entry.Kind == kind);
        for (var i = 0; i < _entries.Count && count > limit;)
        {
            if (_entries[i].Kind == kind)
            {
                _entries.RemoveAt(i);
                count--;
            }
            else
            {
                i++;
            }
        }
    }

    /// <summary>Beyond twenty undoable operations the oldest keeps its text and loses its operation (AF-4.14's limit).</summary>
    private void TrimUndoable()
    {
        var undoable = _entries.Count(entry => entry.Kind == ActionKind.Operation && entry.CanUndo);
        for (var i = 0; i < _entries.Count && undoable > UndoLimit; i++)
        {
            if (_entries[i].Kind == ActionKind.Operation && _entries[i].CanUndo)
            {
                _entries[i].Operation = null;
                undoable--;
            }
        }
    }
}

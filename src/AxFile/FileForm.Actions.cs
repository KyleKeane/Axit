using AxFile.Core;
using Axit.Core;
using Axit.Forms;

namespace AxFile;

/// <summary>
/// The Actions list (SPEC.md AF-9): a second list under the folder list, always there, with every file operation of
/// this window and every change of its marks as one line, in three sections under header lines, the running
/// operations first, then the operations, then the selection; "No actions yet" until the first. Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move
/// between the folder list and it, Escape in it goes back; Enter on a line goes where the action left things, Ctrl+Z
/// on a line takes that one back, Ctrl+I says its number and time. The lines are <see cref="ActionLog"/>; the
/// operations themselves (Operations) begin and end them. Also here: the hand-overs to axMedia, Play folder and Add
/// to queue (AF-5).
/// </summary>
public sealed partial class FileForm
{
    /// <summary>The one line of the Actions list before the first operation (AF-9.1), so that the list is never empty (AX-9.5).</summary>
    private const string NoActionsText = "No actions yet";

    private readonly AxitList _pane = new();
    private readonly ActionLog _actions = new();

    /// <summary>The entry on each row of the list as last filled (null on a header line), so that a line moved to another row is told from one changed in place, and the line the user is on is known once the log has changed.</summary>
    private IReadOnlyList<ActionEntry?> _paneRows = [];

    private void SetUpActions()
    {
        _pane.IntegralHeight = false;
        _pane.Dock = DockStyle.Bottom;
        _pane.TabIndex = 1;
        _pane.AccessibleName = "Actions";
        _pane.DoubleClick += (_, _) => GoToAction();
    }

    /// <summary>The Actions list takes a third of the window at most (AF-9.1), and three lines at least.</summary>
    private void FitPane() => _pane.Height = Math.Max(_pane.ItemHeight * 3 + 4, ClientSize.Height / 3);

    /// <summary>
    /// The list's lines made again from the log (AF-9.2), after the log changed: the sections under their headers,
    /// "No actions yet" while the log is empty. Each line is changed in place while the count is the same, so that
    /// nothing but the changed line moves under NVDA (the running line the user is on is left until it ends, since
    /// rewriting the selected item re-selects it, which NVDA may read again, four times a second for a zip's count);
    /// the whole list otherwise. The newest line is selected while the focus is in the folder list, so that Tab lands
    /// on it; while the focus is here the line the user is on stays selected, found in the rows as last filled, since
    /// lines move between the sections and fall off at the limits (until 2026-10-23 every caller took it before the
    /// change and passed it in). The status bar follows.
    /// </summary>
    private void FillPane()
    {
        var keep = _pane.Focused && _pane.SelectedIndex >= 0 && _pane.SelectedIndex < _paneRows.Count ? _paneRows[_pane.SelectedIndex] : null;
        IReadOnlyList<(string Line, ActionEntry? Entry)> rows = _actions.Count == 0 ? [(NoActionsText, null)] : _actions.Rows();
        var kept = -1;
        for (var i = 0; i < rows.Count; i++)
        {
            if (keep is not null && rows[i].Entry == keep)
            {
                kept = i;
            }
        }

        // The running line the user is on is left alone while it is the same line on the same row; a line that moved to
        // this row is written (until 2026-10-23 the row was skipped by its number, and showed another operation's old
        // text when a line ended and the rows shifted).
        _pane.SetLines(rows.Select(row => row.Line).ToList(),
            i => i == kept && keep!.State == ActionState.Running && i < _paneRows.Count && _paneRows[i] == keep);

        _paneRows = rows.Select(row => row.Entry).ToList();
        var index = kept >= 0 ? kept
            : _pane.Focused ? Math.Clamp(_pane.SelectedIndex, 0, rows.Count - 1)
            : _actions.Latest is { } latest ? Math.Max(0, _actions.IndexOf(latest))
            : 0;
        if (_pane.SelectedIndex != index)
        {
            _pane.SelectedIndex = index;
        }

        UpdateStatusBar();
    }

    /// <summary>Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 (AX-8.2): between the folder list and the Actions list, the two controls of the window.</summary>
    private void SwitchPane()
    {
        if (_list.Focused)
        {
            _pane.Focus();
        }
        else
        {
            _list.Focus();
        }
    }

    /// <summary>Folder → Actions: the focus in the Actions list (AF-9.1).</summary>
    private void ShowActions() => _pane.Focus();

    // ---- the lines (AF-9.2): what the operations call; each changes the log and fills the list again ----

    /// <summary>A running operation's line, "Copying 3 items", in the folder shown.</summary>
    private ActionEntry Begin(string text)
    {
        var entry = _actions.Begin(text, _folder ?? string.Empty);
        FillPane();
        return entry;
    }

    /// <summary>A running line's count, "Compressing, 40 of 120 files".</summary>
    private void Progress(ActionEntry entry, string text)
    {
        _actions.Progress(entry, text);
        FillPane();
    }

    /// <summary>The operation done: its line says what it did and, with an operation, can be undone and leads to its result.</summary>
    private void End(ActionEntry entry, string text, Operation? operation)
    {
        _actions.End(entry, text, operation);
        FillPane();
    }

    /// <summary>The operation failed: "Failed: <reason>" in the list, and the failure said as every failure is (AF-4.5).</summary>
    private void FailAction(ActionEntry entry, string title, string reason)
    {
        _actions.Fail(entry, reason);
        FillPane();
        Failed(title, reason);
    }

    /// <summary>An operation that ran at once (a rename, a new folder): its line, done.</summary>
    private void Record(string text, Operation operation)
    {
        _actions.Add(text, operation);
        FillPane();
    }

    // ---- the list's own keys (AF-9.3): Enter, Ctrl+Z and Ctrl+I on the line, Escape back to the folder list ----

    /// <summary>The line under the selection; null on a header line and on "No actions yet".</summary>
    private ActionEntry? SelectedAction() => _actions.EntryAt(_pane.SelectedIndex);

    /// <summary>A header line or "No actions yet", said again by Enter, Ctrl+Z and Ctrl+I (AF-9.3).</summary>
    private void SayPaneLine() => Announce(_pane.SelectedItem?.ToString() ?? NoActionsText, true);

    /// <summary>
    /// Enter in the Actions list: the folder list shows where the action left things, the entry it made or moved
    /// selected, and takes the focus, which moves before the folder is said, so that NVDA's reading of the list does
    /// not cut the announcement off (AF-3.1; the other way round until 2026-10-23); a running line says it is still
    /// running; the empty line is read again.
    /// </summary>
    private void GoToAction()
    {
        if (SelectedAction() is not { } entry)
        {
            SayPaneLine();
            return;
        }

        if (entry.State == ActionState.Running)
        {
            Announce("Still running", true);
            return;
        }

        var (folder, select) = entry.Location;
        if (folder.Length == 0)
        {
            Announce("Folder not found", true);
            return;
        }

        var changes = !string.Equals(folder, _folder, StringComparison.OrdinalIgnoreCase);
        // A folder that is gone is ShowFolderAsync's "Folder not found" notice and the nearest parent (AF-3.5).
        Do(async () =>
        {
            if (!await ShowFolderAsync(folder, select, remember: false, announce: false))
            {
                return;
            }

            _list.Focus();
            if (changes)
            {
                Announce(FolderAnnouncement(), true);
            }
        });
    }

    /// <summary>Ctrl+Z in the Actions list: the line under the selection taken back, whatever its place (AF-9.3); a line that cannot be says why.</summary>
    private void UndoSelectedAction()
    {
        if (SelectedAction() is not { } entry)
        {
            SayPaneLine();
            return;
        }

        if (!entry.CanUndo)
        {
            Announce(entry.State switch
            {
                ActionState.Running => "Still running",
                ActionState.Undone => "Already undone",
                ActionState.NotUndone => "Not undone: " + entry.Reason,
                _ => "Nothing to undo here",
            }, true);
            return;
        }

        UndoAction(entry);
    }

    /// <summary>Ctrl+I in the Actions list: the line's section, number and time, "Operations, 3 of 12, 21:04"; a header line says its name.</summary>
    private void SayActionInformation()
    {
        var index = _pane.SelectedIndex;
        Announce(_actions.Count > 0 && index >= 0 ? _actions.Information(index) : NoActionsText, true);
    }

    // ---- the hand-overs to axMedia (AF-5): axFile starts the app on the paths and says nothing but the add's count ----

    /// <summary>Item → Play folder in axMedia (Ctrl+Enter): axMedia on the folder shown, its media files from the first; "No sound or video files here" without any.</summary>
    private void PlayFolder()
    {
        if (!TryFolder(out var folder))
        {
            return;
        }

        if (!_shown.Any(entry => !entry.IsFolder && MediaFiles.IsMedia(entry.Path)))
        {
            Announce("No sound or video files here", true);
            return;
        }

        AxitMenu.Start(Dispatch.AxMedia, folder);
    }

    /// <summary>
    /// Item → Add to axMedia's queue (Ctrl+Shift+Q): the entry, or the marked entries (AF-3.7), to the end of axMedia's
    /// queue, a folder as its media files; "Added 12 to axMedia's queue", the focus staying here (AM-D3). The count is
    /// this window's own reading of the folders, on a task.
    /// </summary>
    private void AddToQueue()
    {
        if (NoFolderHere())
        {
            return;
        }

        var targets = Targets();
        if (targets.Count == 0)
        {
            return;
        }

        Do(async () =>
        {
            var count = await Task.Run(() => MediaCount(targets));
            if (count == 0)
            {
                Announce("No sound or video files to add", true);
                return;
            }

            AxitMenu.StartWith(Dispatch.AxMedia, [.. targets.Select(target => target.Path), "--add"]);
            Announce($"Added {count} to axMedia's queue", true);
        });
    }

    /// <summary>How many media files the entries hold: a media file itself, a folder's media files (the hidden ones as the settings say), anything else none.</summary>
    private int MediaCount(List<Entry> targets)
    {
        var count = 0;
        foreach (var target in targets)
        {
            if (!target.IsFolder)
            {
                count += MediaFiles.IsMedia(target.Path) ? 1 : 0;
                continue;
            }

            try
            {
                count += FolderListing.Read(target.Path, _settings.ShowHidden, _settings.ShowDotFiles).Entries.Count(entry => !entry.IsFolder && MediaFiles.IsMedia(entry.Path));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Error("A folder could not be read for the queue", ex);
            }
        }

        return count;
    }
}

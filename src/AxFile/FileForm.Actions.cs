using AxFile.Core;
using Axit.Core;
using Axit.Forms;

namespace AxFile;

/// <summary>
/// The Actions list (SPEC.md AF-9): a second list under the folder list, always there, with every file operation of
/// this window as one line, "No actions yet" until the first. Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move
/// between the folder list and it, Escape in it goes back; Enter on a line goes where the action left things, Ctrl+Z
/// on a line takes that one back, Ctrl+I says its number and time. The lines are <see cref="ActionLog"/>; the
/// operations themselves (Operations) begin and end them. Also here: the hand-overs to AxMedia, Play folder and Add
/// to queue (AF-5).
/// </summary>
public sealed partial class FileForm
{
    /// <summary>The one line of the Actions list before the first operation (AF-9.1), so that the list is never empty (AX-9.5).</summary>
    private const string NoActionsText = "No actions yet";

    private readonly ListBox _pane = new();
    private readonly ActionLog _actions = new();

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
    /// The list's lines made again from the log (AF-9.2), "No actions yet" while it is empty: each line changed in
    /// place while the count is the same, so that nothing but the changed line moves under NVDA (a running line under
    /// the focus is left until it ends, since rewriting the selected item re-selects it, which NVDA may read again,
    /// four times a second for a zip's count); the whole list otherwise. The newest line is selected while the focus
    /// is in the folder list, so that Tab lands on it; while the focus is here the line the user is on stays selected
    /// (<paramref name="keep"/>, taken before the log changed, since the oldest line falls off at the limit and every
    /// index shifts). The status bar follows.
    /// </summary>
    private void FillPane(ActionEntry? keep)
    {
        IReadOnlyList<string> lines = _actions.Count == 0 ? [NoActionsText] : _actions.Lines();
        var entries = _actions.Entries;
        if (lines.Count == _pane.Items.Count)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                // A running line under the focus keeps its first text (AF-9.2), except the "No actions yet" line it replaces.
                if (_pane.Focused && i == _pane.SelectedIndex && i < entries.Count && entries[i].State == ActionState.Running
                    && !string.Equals(_pane.Items[i].ToString(), NoActionsText, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.Equals(_pane.Items[i].ToString(), lines[i], StringComparison.Ordinal))
                {
                    _pane.Items[i] = lines[i];
                }
            }
        }
        else
        {
            _pane.BeginUpdate();
            _pane.Items.Clear();
            _pane.Items.AddRange(lines.Cast<object>().ToArray());
            _pane.EndUpdate();
        }

        var kept = keep is null ? -1 : _actions.IndexOf(keep);
        var index = kept >= 0 ? kept : _pane.Focused ? Math.Clamp(_pane.SelectedIndex, 0, lines.Count - 1) : lines.Count - 1;
        if (_pane.SelectedIndex != index)
        {
            _pane.SelectedIndex = index;
        }

        UpdateStatusBar();
    }

    /// <summary>The line the user is on while the Actions list has the focus, taken before the log changes; null otherwise.</summary>
    private ActionEntry? Keep() => _pane.Focused ? SelectedAction() : null;

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

    // ---- the lines (AF-9.2): what the operations call ----

    /// <summary>A running operation's line, "Copying 3 items", in the folder shown.</summary>
    private ActionEntry Begin(string text)
    {
        var keep = Keep();
        var entry = _actions.Begin(text, _folder ?? string.Empty);
        FillPane(keep);
        return entry;
    }

    /// <summary>A running line's count, "Compressing, 40 of 120 files".</summary>
    private void Progress(ActionEntry entry, string text)
    {
        var keep = Keep();
        _actions.Progress(entry, text);
        FillPane(keep);
    }

    /// <summary>The operation done: its line says what it did and, with an operation, can be undone and leads to its result.</summary>
    private void End(ActionEntry entry, string text, Operation? operation)
    {
        var keep = Keep();
        _actions.End(entry, text, operation);
        FillPane(keep);
    }

    /// <summary>The operation failed: "Failed: <reason>" in the list, and the failure said as every failure is (AF-4.5).</summary>
    private void FailAction(ActionEntry entry, string title, string reason)
    {
        var keep = Keep();
        _actions.Fail(entry, reason);
        FillPane(keep);
        Failed(title, reason);
    }

    /// <summary>An operation that ran at once (a rename, a new folder): its line, done.</summary>
    private void Record(string text, Operation operation)
    {
        var keep = Keep();
        _actions.Add(text, operation);
        FillPane(keep);
    }

    /// <summary>A line's state changed by an undo (undone, or refused): the list made again with the user's line kept.</summary>
    private void RefreshPane() => FillPane(Keep());

    // ---- the list's own keys (AF-9.3): Enter, Ctrl+Z and Ctrl+I on the line, Escape back to the folder list ----

    /// <summary>The line under the selection; null on "No actions yet".</summary>
    private ActionEntry? SelectedAction() =>
        _pane.SelectedIndex is var i && i >= 0 && i < _actions.Count ? _actions.Entries[i] : null;

    /// <summary>Enter in the Actions list: the folder list shows where the action left things, the entry it made or moved selected, and takes the focus; a running line says it is still running; the empty line is read again.</summary>
    private void GoToAction()
    {
        if (SelectedAction() is not { } entry)
        {
            Announce(NoActionsText, true);
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

        // A folder that is gone is ShowFolderAsync's "Folder not found" notice and the nearest parent (AF-3.5).
        Do(async () =>
        {
            if (await ShowFolderAsync(folder, select, remember: false))
            {
                _list.Focus();
            }
        });
    }

    /// <summary>Ctrl+Z in the Actions list: the line under the selection taken back, whatever its place (AF-9.3); a line that cannot be says why.</summary>
    private void UndoSelectedAction()
    {
        if (SelectedAction() is not { } entry)
        {
            Announce(NoActionsText, true);
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

    /// <summary>Ctrl+I in the Actions list: the line's number and time, "3 of 12, 21:04".</summary>
    private void SayActionInformation()
    {
        var index = _pane.SelectedIndex;
        Announce(index >= 0 && index < _actions.Count ? _actions.Information(index) : NoActionsText, true);
    }

    // ---- the hand-overs to AxMedia (AF-5): AxFile starts the app on the paths and says nothing but the add's count ----

    /// <summary>Item → Play folder in AxMedia (Ctrl+Enter): AxMedia on the folder shown, its media files from the first; "No sound or video files here" without any.</summary>
    private void PlayFolder()
    {
        if (NoFolderHere() || _folder is not { } folder)
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
    /// Item → Add to AxMedia's queue (Ctrl+Shift+Q): the entry, or the marked entries (AF-3.7), to the end of AxMedia's
    /// queue, a folder as its media files; "Added 12 to AxMedia's queue", the focus staying here (AM-D3). The count is
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
            Announce($"Added {count} to AxMedia's queue", true);
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

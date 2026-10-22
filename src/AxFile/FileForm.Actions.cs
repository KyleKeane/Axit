using AxFile.Core;
using Axit.Core;
using Axit.Forms;

namespace AxFile;

/// <summary>
/// The Actions list (SPEC.md AF-9): a second list under the folder list with every file operation of this window as
/// one line, shown the first time an operation runs and kept for the window's life. Tab, Shift+Tab, Ctrl+Tab,
/// Ctrl+Shift+Tab and F6 move between the folder list and it, Escape in it goes back; Enter on a line goes where the
/// action left things, Ctrl+Z on a line takes that one back, Ctrl+I says its number and time. The lines are
/// <see cref="ActionLog"/>; the operations themselves (Operations) begin and end them. Also here: the hand-overs to
/// AxMedia, Play folder and Add to queue (AF-5).
/// </summary>
public sealed partial class FileForm
{
    private readonly ListBox _pane = new();
    private readonly ActionLog _actions = new();

    /// <summary>Whether the Actions list has been shown (AF-9.1): once it has, it stays, hidden only while a notice shows.</summary>
    private bool _paneShown;

    private void SetUpActions()
    {
        _pane.IntegralHeight = false;
        _pane.Dock = DockStyle.Bottom;
        _pane.Visible = false;
        _pane.TabIndex = 1;
        _pane.AccessibleName = "Actions";
        _pane.DoubleClick += (_, _) => GoToAction();
    }

    /// <summary>The Actions list takes a third of the window at most (AF-9.1), and three lines at least.</summary>
    private void FitPane() => _pane.Height = Math.Max(_pane.ItemHeight * 3 + 4, ClientSize.Height / 3);

    /// <summary>The Actions list shown the first time a line is there for it; nothing is said for it (the operation's own announcement is what is heard).</summary>
    private void ShowPane()
    {
        _paneShown = true;
        if (!NoticeOpen && !_pane.Visible)
        {
            FitPane();
            _pane.Visible = true;
        }
    }

    /// <summary>
    /// The list's lines made again from the log (AF-9.2): each line changed in place while the count is the same, so
    /// that the selection stays where it is and nothing but the changed line moves under NVDA; the whole list
    /// otherwise, with the newest line selected while the focus is in the folder list, so that Tab lands on it, and
    /// the selection kept by index while the focus is here. The status bar follows.
    /// </summary>
    private void FillPane()
    {
        var lines = _actions.Lines();
        if (lines.Count == _pane.Items.Count)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                if (!string.Equals(_pane.Items[i].ToString(), lines[i], StringComparison.Ordinal))
                {
                    _pane.Items[i] = lines[i];
                }
            }
        }
        else
        {
            var index = _pane.Focused ? _pane.SelectedIndex : lines.Count - 1;
            _pane.BeginUpdate();
            _pane.Items.Clear();
            _pane.Items.AddRange(lines.Cast<object>().ToArray());
            _pane.EndUpdate();
            if (lines.Count > 0)
            {
                _pane.SelectedIndex = Math.Clamp(index, 0, lines.Count - 1);
            }
        }

        UpdateStatusBar();
    }

    /// <summary>Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 (AX-8.2): between the folder list and the Actions list; the folder list alone while no Actions list shows.</summary>
    private void SwitchPane()
    {
        if (_pane.Visible && _list.Focused)
        {
            _pane.Focus();
        }
        else
        {
            _list.Focus();
        }
    }

    /// <summary>Folder → Actions: the Actions list with the focus in it, or "No actions yet" (AF-9.1).</summary>
    private void ShowActions()
    {
        if (_actions.Count == 0)
        {
            Announce("No actions yet", true);
            return;
        }

        ShowPane();
        _pane.Focus();
    }

    // ---- the lines (AF-9.2): what the operations call ----

    /// <summary>A running operation's line, "Copying 3 items", in the folder shown; the list appears with it.</summary>
    private ActionEntry Begin(string text)
    {
        var entry = _actions.Begin(text, _folder ?? string.Empty);
        ShowPane();
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
    private void Fail(ActionEntry entry, string title, string reason)
    {
        _actions.Fail(entry, reason);
        FillPane();
        Failed(title, reason);
    }

    /// <summary>An operation that ran at once (a rename, a new folder): its line, done.</summary>
    private void Record(string text, Operation operation)
    {
        _actions.Add(text, operation);
        ShowPane();
        FillPane();
    }

    // ---- the list's own keys (AF-9.3): Enter, Ctrl+Z and Ctrl+I on the line, Escape back to the folder list ----

    private ActionEntry? SelectedAction() =>
        _pane.SelectedIndex is var i && i >= 0 && i < _actions.Count ? _actions.Entries[i] : null;

    /// <summary>Enter in the Actions list: the folder list shows where the action left things, the entry it made or moved selected, and takes the focus; a running line says it is still running.</summary>
    private void GoToAction()
    {
        if (SelectedAction() is not { } entry)
        {
            Announce("No actions yet", true);
            return;
        }

        if (entry.State == ActionState.Running)
        {
            Announce("Still running", true);
            return;
        }

        var (folder, select) = entry.Location;
        if (folder.Length == 0 || !Directory.Exists(folder))
        {
            Announce("Folder not found", true);
            return;
        }

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
            Announce("No actions yet", true);
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
        Announce(index >= 0 && index < _actions.Count ? _actions.Information(index) : "No actions yet", true);
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

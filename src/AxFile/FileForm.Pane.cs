using AxFile.Core;
using Axit.Core;

namespace AxFile;

/// <summary>
/// The pane (SPEC.md AF-9): a second control under the folder list, one thing at a time and named after it: the
/// player's queue with the operations' status lines under it (a list). It appears when it first has something to
/// show and stays for the window's life; Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move between the folder
/// list and it, Escape in it goes back to the list. Each command's result is one announcement on the control that
/// has the focus; nothing is spoken on an arrow key (AF-D7).
/// </summary>
public sealed partial class FileForm
{
    private readonly ListBox _pane = new();
    private readonly PlayQueue _queue = new();

    /// <summary>Whether the pane has been shown (AF-9.1): once it has, it stays, hidden only while a notice shows.</summary>
    private bool _paneShown;

    private void SetUpPane()
    {
        _pane.IntegralHeight = false;
        _pane.Dock = DockStyle.Bottom;
        _pane.Visible = false;
        _pane.TabIndex = 1;
        _pane.AccessibleName = "Queue";
        _pane.Items.Add(PlayQueue.EmptyText);
        _pane.DoubleClick += (_, _) => PlayPaneItem();
    }

    /// <summary>The pane takes a third of the window at most (AF-9.1), and three lines at least.</summary>
    private void FitPane() => _pane.Height = Math.Max(_pane.ItemHeight * 3 + 4, ClientSize.Height / 3);

    /// <summary>The pane shown with the queue list in it, the first time something is there for it; the focus stays where it is.</summary>
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
    /// The queue list's lines made again from the queue (AF-9.2): each line changed in place while the count is the
    /// same, so that the selection stays where it is and nothing but the changed line moves under NVDA; the whole
    /// list otherwise, the selection kept by index.
    /// </summary>
    private void FillPane()
    {
        var lines = _queue.Lines(PlayerActive);
        if (lines.Count == _pane.Items.Count)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                if (!string.Equals(_pane.Items[i].ToString(), lines[i], StringComparison.Ordinal))
                {
                    _pane.Items[i] = lines[i];
                }
            }

            return;
        }

        var index = _pane.SelectedIndex;
        _pane.BeginUpdate();
        _pane.Items.Clear();
        _pane.Items.AddRange(lines.Cast<object>().ToArray());
        _pane.EndUpdate();
        _pane.SelectedIndex = Math.Clamp(index, 0, _pane.Items.Count - 1);
    }

    private bool PlayerActive => _player is { State: PlayerState.Playing or PlayerState.Paused or PlayerState.Opening };

    /// <summary>Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 (AX-8.2): between the folder list and the pane; the list alone while no pane shows.</summary>
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

    // ---- the queue's commands (AF-9.2) ----

    /// <summary>Player → Play folder (Ctrl+Enter in the list): the folder's media files replace the queue and play from the first; "No sound or video files here" without any.</summary>
    private void PlayFolder()
    {
        if (NoFolderHere())
        {
            return;
        }

        var paths = MediaFiles.Queue(_shown);
        if (paths.Count == 0)
        {
            Announce("No sound or video files here", true);
            return;
        }

        _queue.Set(paths, 0);
        ShowPane();
        FillPane();
        PlayCurrent(prompted: true);
    }

    /// <summary>
    /// Player → Add to queue (Ctrl+Shift+Q in the list): the entry, or the marked entries (AF-3.7), appended: a
    /// media file itself, a folder's media files in order; "Added 12 to the queue". Nothing starts playing.
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
            var paths = await Task.Run(() => MediaOf(targets));
            var added = _queue.Add(paths);
            ShowPane();
            FillPane();
            Announce(added == 0 ? "No sound or video files to add" : $"Added {added} to the queue", true);
        });
    }

    /// <summary>The media files of the entries: a media file itself, a folder's media files in the list's order (the hidden ones as the settings say), anything else nothing.</summary>
    private List<string> MediaOf(List<Entry> targets)
    {
        var paths = new List<string>();
        foreach (var target in targets)
        {
            if (!target.IsFolder)
            {
                if (MediaFiles.IsMedia(target.Path))
                {
                    paths.Add(target.Path);
                }

                continue;
            }

            try
            {
                paths.AddRange(MediaFiles.Queue(FolderListing.Read(target.Path, _settings.ShowHidden, _settings.ShowDotFiles).Entries));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Error("A folder could not be read for the queue", ex);
            }
        }

        return paths;
    }

    /// <summary>Player → Clear queue: the files and the status lines gone and the player stopped; "Queue cleared".</summary>
    private void ClearQueue()
    {
        _queue.Clear();
        StopPlayback();
        ShowPane();
        FillPane();
        Announce("Queue cleared", true);
    }

    /// <summary>Player → Show queue: the pane with the queue list, the focus in it.</summary>
    private void ShowQueue()
    {
        ShowPane();
        FillPane();
        _pane.Focus();
    }

    // ---- the pane's own keys (AF-9.6): Enter, Delete and Ctrl+I on the line, Escape back to the list ----

    /// <summary>Enter in the queue: the file plays, "Playing <name>"; a status line, or the empty line, is read again.</summary>
    private void PlayPaneItem()
    {
        var index = _pane.SelectedIndex;
        if (_queue.Select(index))
        {
            PlayCurrent(prompted: true);
            return;
        }

        Announce(PaneLine(index), true);
    }

    /// <summary>Delete in the queue: the line taken out, "Removed <name>"; the file playing goes on to the next one, or stops at the end.</summary>
    private void RemovePaneItem()
    {
        var index = _pane.SelectedIndex;
        var wasPlaying = _queue.IsFile(index) && index == _queue.Playing && PlayerActive;
        if (_queue.Remove(index) is not { } removed)
        {
            Announce(PaneLine(index), true);
            return;
        }

        if (wasPlaying)
        {
            if (_queue.PlayingPath is not null)
            {
                PlayCurrent(prompted: false);
            }
            else
            {
                StopPlayback();
            }
        }

        FillPane();
        Announce($"Removed {removed}", true);
    }

    /// <summary>Ctrl+I in the queue: the file's name and place, "03 Chapter 3.mp3, 3 of 12", with the position while it is the one playing; a status line is read again.</summary>
    private void SayPaneInformation()
    {
        var index = _pane.SelectedIndex;
        if (!_queue.IsFile(index))
        {
            Announce(PaneLine(index), true);
            return;
        }

        var text = _queue.Information(index);
        if (index == _queue.Playing && _player is { State: PlayerState.Playing or PlayerState.Paused } player)
        {
            text += $", {(player.State == PlayerState.Playing ? "playing" : "paused")}, {MediaFiles.FormatTime(player.Position)} of {MediaFiles.FormatTime(player.Duration)}";
        }

        Announce(text, true);
    }

    private string PaneLine(int index) => index >= 0 && index < _pane.Items.Count ? _pane.Items[index].ToString() ?? PlayQueue.EmptyText : PlayQueue.EmptyText;

    // ---- the operations' status lines (AF-9.3) ----

    /// <summary>An operation's line in the pane: a new line for a token of -1, or that token's line replaced ("Copying 3 items", then "Copied 3 items"); the pane shown; the token to report with next.</summary>
    private int Report(int token, string text)
    {
        token = _queue.Report(token, text);
        ShowPane();
        FillPane();
        return token;
    }
}

using AxFile.Core;
using Axit.Core;
using Axit.Forms;

namespace AxFile;

/// <summary>
/// The pane (SPEC.md AF-9): a second control under the folder list, one thing at a time and named after it: the
/// player's queue with the operations' status lines under it (a list, AF-9.2, AF-9.3), or a file's text to read (a
/// read-only text, AF-9.4). It appears when it first has something to show and stays for the window's life; Tab,
/// Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move between the folder list and it, Escape in it goes back to the
/// list. Each command's result is one announcement on the control that has the focus; nothing is spoken on an arrow
/// key (AF-D7).
/// </summary>
public sealed partial class FileForm
{
    private readonly ListBox _pane = new();
    private readonly TextBox _preview = new();
    private readonly PlayQueue _queue = new();

    /// <summary>Whether the pane has been shown (AF-9.1): once it has, it stays, hidden only while a notice shows.</summary>
    private bool _paneShown;

    /// <summary>Whether the pane holds a file's text now rather than the queue (AF-9.4).</summary>
    private bool _previewing;

    private void SetUpPane()
    {
        _pane.IntegralHeight = false;
        _pane.Dock = DockStyle.Bottom;
        _pane.Visible = false;
        _pane.TabIndex = 1;
        _pane.AccessibleName = "Queue";
        _pane.Items.Add(PlayQueue.EmptyText);
        _pane.DoubleClick += (_, _) => PlayPaneItem();

        // AF-9.4: a read-only text with what every read-only text of the bundle has (AX-7.2): the page and paragraph
        // keys, the Markdown jumps of the window's table, l for the line; Ctrl+A and Ctrl+C are the control's own.
        _preview.Multiline = true;
        _preview.ReadOnly = true;
        _preview.WordWrap = true;
        _preview.ScrollBars = ScrollBars.Vertical;
        _preview.HideSelection = false;
        _preview.MaxLength = 0;
        _preview.BackColor = SystemColors.Window;
        _preview.ForeColor = SystemColors.WindowText;
        _preview.Dock = DockStyle.Bottom;
        _preview.Visible = false;
        _preview.TabIndex = 1;
        _preview.AccessibleName = "Preview";
        _preview.KeyDown += EditKeys.Handle;
        _preview.KeyDown += OnPreviewKeyDown;
    }

    /// <summary>The control the pane shows now: the queue list, or the preview while a file's text is there.</summary>
    private Control PaneControl => _previewing ? _preview : _pane;

    /// <summary>The pane takes a third of the window at most (AF-9.1), and three lines at least.</summary>
    private void FitPane()
    {
        var height = Math.Max(_pane.ItemHeight * 3 + 4, ClientSize.Height / 3);
        _pane.Height = height;
        _preview.Height = height;
    }

    /// <summary>The pane shown with the queue list in it the first time something is there for it; a preview being read is left in place (the list comes back with the next queue command).</summary>
    private void ShowPane()
    {
        _paneShown = true;
        if (!NoticeOpen && !_previewing && !_pane.Visible)
        {
            FitPane();
            _pane.Visible = true;
        }
    }

    /// <summary>The queue list brought back in place of a preview (AF-9.4), the focus with it when the preview had it; the pane shown.</summary>
    private void ShowQueueList()
    {
        if (_previewing)
        {
            var had = _preview.Focused;
            _previewing = false;
            FitPane();
            _pane.Visible = !NoticeOpen;
            _preview.Visible = false;
            _preview.Clear();
            if (had)
            {
                _pane.Focus();
            }
        }

        ShowPane();
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
        var pane = PaneControl;
        if (pane.Visible && _list.Focused)
        {
            pane.Focus();
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
        ShowQueueList();
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
            ShowQueueList();
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
        ShowQueueList();
        FillPane();
        Announce("Queue cleared", true);
    }

    /// <summary>Player → Show queue: the pane with the queue list, the focus in it.</summary>
    private void ShowQueue()
    {
        ShowQueueList();
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

    // ---- a file to read (AF-9.4) ----

    /// <summary>
    /// Item → Preview here (Ctrl+Shift+P in the list): the file's text in the pane as a read-only text named
    /// "<name>, preview", the focus on its first line; a file over a megabyte shows its first megabyte and says so in
    /// the name; "Not a text file" for anything else, "Not a file" on a folder. The folder list keeps its place, so
    /// Escape returns to the entry.
    /// </summary>
    private void ShowPreview()
    {
        if (CurrentEntry() is not { } entry)
        {
            SayNoEntry();
            return;
        }

        if (entry.IsFolder)
        {
            Announce("Not a file", true);
            return;
        }

        Do(async () =>
        {
            PreviewText? text;
            try
            {
                text = await Task.Run(() => Preview.Read(entry.Path));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Failed("Preview failed", FileOperations.Message(ex));
                return;
            }

            if (text is null)
            {
                Announce("Not a text file", true);
                return;
            }

            _previewing = true;
            _paneShown = true;
            _preview.AccessibleName = $"{entry.Name}, preview" + (text.Cut ? ", first megabyte" : string.Empty);
            _preview.Text = text.Text;
            _preview.Select(0, 0);
            FitPane();
            _pane.Visible = false;
            _preview.Visible = !NoticeOpen;
            _preview.Focus();
        });
    }

    /// <summary>The preview's own keys: the Markdown jumps of the table (AF-9.4) and l for the line, "Line 12 of 40".</summary>
    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (e.KeyData == AxFileKeys.LineNumber)
        {
            e.Handled = e.SuppressKeyPress = true;
            Announce(PreviewLine(), true);
            return;
        }

        if (TextJumps.Handle(_preview, AxFileKeys.PreviewJumps, e.KeyData, Announce))
        {
            e.Handled = e.SuppressKeyPress = true;
        }
    }

    /// <summary>"Line 12 of 40": the caret's line of the preview's text and the number of lines.</summary>
    private string PreviewLine()
    {
        var text = _preview.Text;
        var caret = Math.Min(_preview.SelectionStart, text.Length);
        var line = 1;
        var lines = 1;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                lines++;
                if (i < caret)
                {
                    line++;
                }
            }
        }

        return $"Line {line} of {lines}";
    }
}

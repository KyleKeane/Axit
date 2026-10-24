using AxMedia.Core;
using Axit.Core;
using Axit.Forms;

namespace AxMedia;

/// <summary>
/// axMedia's window (SPEC.md AM-2): a menu bar, the video area shown only while a video plays, one list with the
/// queue, and a status bar with what plays and where. Enter plays the file under the selection and the rest follow;
/// the transport keys work from anywhere in the window. The engine is <see cref="MediaPlayer"/>, its events posted
/// back to this thread; the queue is <see cref="PlayQueue"/>. A start with paths while this window is open arrives as
/// a hand-over message (AM-1.2, <see cref="AppMessage"/>) in <see cref="WndProc"/>. The notices, the fonts, the
/// window's place, the update flow, find and the announcements are the bundle's window base (<see cref="AxitForm"/>).
/// Grown from axFile's player and queue pane (docs/axit/axfile-split.md).
/// </summary>
public sealed partial class MediaForm : AxitForm
{
    private readonly MediaSettings _settings;
    private readonly HandOver? _start;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _recentItem = new("Open &recent");
    private readonly AxitList _list = new();
    private readonly Panel _video = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _stateLabel;
    private readonly ToolStripStatusLabel _placeLabel;
    private readonly System.Windows.Forms.Timer _clock = new() { Interval = 1000 };
    private readonly PlayQueue _queue = new();
    private MediaPlayer? _player;

    /// <summary>How the file now opening was asked for: by the user (said, interrupting), or by the queue moving on (said when the setting says, not interrupting).</summary>
    private bool _prompted;

    /// <summary>The hand-overs waiting to be applied, in the order they came, one at a time (AM-1.3); <see cref="_applying"/> while one is.</summary>
    private readonly Queue<HandOver> _handOvers = new();
    private bool _applying;

    /// <param name="start">What the command line asked for (AM-1.1): a play or an add with its paths; null for an empty queue.</param>
    /// <param name="settingsPath">Where the settings are saved; the app's own file unless a probe says otherwise, so that a probe never writes the user's.</param>
    public MediaForm(HandOver? start, MediaSettings settings, string? settingsError, string? settingsPath = null)
        : base(AxMediaApp.Name, settings, settingsPath ?? MediaSettings.DefaultPath, settingsError)
    {
        _settings = settings;
        _start = start;
        // A start on one file is named after it from the first moment, so that NVDA reads the file's window as it opens (AM-2.3).
        Text = start is { Verb: AppMessage.Play, Paths: [var only] } && File.Exists(only) ? $"{Path.GetFileName(only)} - {AxMediaApp.Name}" : AxMediaApp.Name;

        // ---- menus and keys (AM-5.1, AM-5.2): one command list (docs/axit/app-kit.md AK-2), each key from the tables ----
        // Queue is the thing worked on, in the standard order of a first menu (bundle AX-6): New and Open with the
        // recent list, the items on the file, Show current file in, Exit. Enter, Delete and Space act in the list
        // only (AM-5.2); in a notice's field they are the field's own, and in the list returning true keeps it from dinging.
        var queue = new ToolStripMenuItem("&Queue");
        Add(queue, new Command("&New window", AxMediaKeys.NewWindow, NewWindow) { Mark = CommandMark.Open });
        Add(queue, new Command("&Open files...", AxMediaKeys.OpenFiles, OpenFiles) { Mark = CommandMark.Open });
        Add(queue, new Command("Open &folder...", AxMediaKeys.OpenFolder, OpenFolder) { Mark = CommandMark.Open });
        CommandPalette.FilledOnOpening(_recentItem).DropDownOpening += (_, _) => FillRecent();
        FillRecent();
        queue.DropDownItems.Add(_recentItem);
        queue.DropDownItems.Add(new ToolStripSeparator());
        Add(queue, new Command("&Play", AxMediaKeys.Play, PlaySelected) { When = () => _list.Focused, Mark = CommandMark.Open });
        Add(queue, new Command("R&emove", AxMediaKeys.Remove, RemoveSelected) { When = () => _list.Focused });
        queue.DropDownItems.Add(new ToolStripMenuItem("&Clear queue", null, (_, _) => ClearQueue()));
        queue.DropDownItems.Add(new ToolStripSeparator());
        Add(queue, new Command("&Information", AxMediaKeys.Information, SayInformation) { Mark = CommandMark.Read });
        // AM-1.4: the ways out, on the file playing or the one under the selection; the File Explorer item is the
        // first of the submenu the base makes, so its command is made here and its key added to the list.
        var explorer = new Command("File &Explorer", AxMediaKeys.ShowInExplorer, ShowInExplorer) { Mark = CommandMark.Open };
        queue.DropDownItems.Add(CreateShowInMenu("&Show current file in", Add(explorer), CurrentFile));
        queue.DropDownItems.Add(new ToolStripSeparator());
        queue.DropDownItems.Add(CreateExitItem());

        // The menu item shows Space, the list's key; Ctrl+P pauses and resumes from anywhere in the window.
        var play = new ToolStripMenuItem("&Play");
        Add(play, new Command("P&ause or resume", AxMediaKeys.PauseResume, PauseResume) { When = () => _list.Focused, Mark = CommandMark.Open });
        AddKey(new Command("Pause or resume", AxMediaKeys.PauseResumeAlso, PauseResume) { Mark = CommandMark.Open });
        Add(play, new Command("&Next file", AxMediaKeys.NextFile, () => MoveInQueue(1)) { Mark = CommandMark.Open });
        Add(play, new Command("Pre&vious file", AxMediaKeys.PreviousFile, () => MoveInQueue(-1)) { Mark = CommandMark.Open });
        Add(play, new Command($"&Forward {_settings.SeekSeconds} seconds", AxMediaKeys.Forward, () => Seek(_settings.SeekSeconds)) { Mark = CommandMark.Open });
        Add(play, new Command($"&Back {_settings.SeekSeconds} seconds", AxMediaKeys.Back, () => Seek(-_settings.SeekSeconds)) { Mark = CommandMark.Open });
        Add(play, new Command("&Go to time...", AxMediaKeys.GoToTime, GoToTime) { Mark = CommandMark.Open });
        play.DropDownItems.Add(new ToolStripSeparator());
        Add(play, new Command("Fa&ster", AxMediaKeys.Faster, () => ChangeSpeed(MediaFiles.Faster(_settings.Speed))));
        Add(play, new Command("S&lower", AxMediaKeys.Slower, () => ChangeSpeed(MediaFiles.Slower(_settings.Speed))));
        play.DropDownItems.Add(new ToolStripMenuItem("N&ormal speed", null, (_, _) => ChangeSpeed(1)));
        play.DropDownItems.Add(new ToolStripSeparator());
        play.DropDownItems.Add(Toggle("Say &each file as it starts", _settings.SayEachFile, v => _settings.SayEachFile = v));
        Add(play, new Command("&What is playing", AxMediaKeys.WhatIsPlaying, SayWhatIsPlaying) { Mark = CommandMark.Read });

        // The keys without a menu item: Tab and its kin back to the list (the one control), and find in the queue.
        foreach (var key in new[] { BundleKeys.NextControl, BundleKeys.PreviousControl, BundleKeys.NextControlAlso })
        {
            AddKey(new Command("Back to the list", key, () => _list.Focus()) { Mark = CommandMark.Open });
        }

        AddKey(new Command("Find in the queue", BundleKeys.Find, () => ShowFindInList(_list, Names)) { Mark = CommandMark.Read });
        AddKey(new Command("Find next", BundleKeys.FindNext, () => FindInList(_list, Names, 1)) { Mark = CommandMark.Read });
        AddKey(new Command("Find previous", BundleKeys.FindPrevious, () => FindInList(_list, Names, -1)) { Mark = CommandMark.Read });

        // Options, as the other windows' (AX-6).
        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "&Media resources",
        [
            ("&Formats Windows plays, at Microsoft", "https://learn.microsoft.com/windows/win32/medfound/supported-media-formats-in-media-foundation"),
            ("Free &codecs in the Microsoft Store", "https://apps.microsoft.com/search?query=media+extensions"),
        ]);

        // The bundle's Axit menu (AX-9) sits before Help in every window and ends with the update items and About.
        _menu.Items.AddRange([queue, play, options, CreateAxitMenu(), help]);
        MainMenuStrip = _menu;

        // ---- the list (AM-2.1): the one control; its own keys stay native ----
        _list.IntegralHeight = false;
        _list.Dock = DockStyle.Fill;
        _list.TabIndex = 0;
        _list.AccessibleName = _queue.ListName;
        _list.Items.Add(PlayQueue.EmptyText);
        _list.SelectedIndex = 0;
        _list.DoubleClick += (_, _) => PlaySelected();
        // The bundle's list (AX-8.6): the empty queue's one line is no item to type to.
        _list.Names = () => _queue.Count == 0 ? [] : _list.Items.Cast<object>().Take(_queue.Count).Select(item => item.ToString() ?? string.Empty).ToList();

        // ---- the video area (AM-2.4): a plain surface at the top, shown while a video plays, never focused ----
        _video.Dock = DockStyle.Top;
        _video.Height = 0;
        _video.Visible = false;
        // The one colour an app sets (AX-7.7, AM-2.4): a video surface is black everywhere, so that a picture's
        // letterbox is not the window's colour (decided 2026-10-23).
        _video.BackColor = Color.Black;
        _video.TabStop = false;
        _video.AccessibleName = "Video";
        _video.AccessibleRole = AccessibleRole.Graphic;
        _video.Resize += (_, _) => _player?.UpdateVideo();
        _video.Paint += (_, _) => _player?.UpdateVideo();

        // ---- the status bar (AM-2.2): the bundle's pair, no AccessibleName, no Spring ----
        (_stateLabel, _placeLabel) = StatusLayout.Pair(_status);
        _clock.Tick += (_, _) => UpdateStatusBar();
        SizeChanged += (_, _) => FitVideo();

        // Docked from the last control to the first: the menu takes the top, the status bar the bottom, the video
        // area the strip under the menu while it shows, and the list (or the notice over it) what is left.
        Controls.AddRange([Overlay, _list, _video, _status, _menu]);

        ApplyTextFont();
        RestoreWindow();
        UpdateStatusBar();
    }

    // ---- what the window base asks for ----

    /// <summary>A window opened by an add (axFile's Ctrl+Shift+Q with no axMedia open) stays behind: the user asked to add, not to come here (AM-D3).</summary>
    protected override bool ShowWithoutActivation => _start is { Verb: AppMessage.Add };

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice()
    {
        _list.Visible = false;
        _video.Visible = false;
    }

    protected override void ShowAfterNotice()
    {
        _list.Visible = true;
        FitVideo();
    }

    protected override Control FocusTarget => _list;

    protected override void ApplyFont(Font font) => _list.Font = font;

    /// <summary>After Update and restart: the file in hand again, with its folder (AM-1.1); an empty window otherwise.</summary>
    protected override IReadOnlyList<string> RestartArguments() => FileInHand is { } path ? [Dispatch.AxMedia, path] : [Dispatch.AxMedia];

    protected override string UpdateRestartText => $"closes {AxMediaApp.Name}, installs it and starts it again on the file playing";

    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Queue: {_queue.Count} files, playing {_queue.PlayingName ?? "(none)"}",
        $"Player: {_player?.State.ToString() ?? "not started"}, speed {Speed}",
    ];

    protected override void OnShown(EventArgs e)
    {
        if (!NoticeOpen)
        {
            _list.Select();
        }

        base.OnShown(e);
        if (_start is { } start)
        {
            Apply(start);
        }
    }

    /// <summary>The file in hand, for Recently closed (AX-9.4); chosen there, it plays with its folder again.</summary>
    protected override string? PathInHand => FileInHand;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // The base first, while the file playing is still the one in hand; AM-4.4: closing the window stops the player.
        base.OnFormClosing(e);
        StopPlayer();
    }

    /// <summary>AM-1.2, AM-1.3: a start with paths while this window is open hands them here; the base answers the message at once and the paths are applied after, nothing read before the answer.</summary>
    protected override Action? TakeHandOver(HandOver message) => () => Apply(message);

    /// <summary>A hand-over (or the start) queued and applied after the ones before it, in the order they came (one queue drained in turn since 2026-10-23; a chain of tasks before); one that fails is its own notice, and the next applies.</summary>
    private void Apply(HandOver message)
    {
        _handOvers.Enqueue(message);
        if (!_applying)
        {
            Do(DrainAsync);
        }
    }

    private async Task DrainAsync()
    {
        _applying = true;
        try
        {
            while (_handOvers.TryDequeue(out var message))
            {
                await ApplyAsync(message);
            }
        }
        finally
        {
            _applying = false;
            if (_handOvers.Count > 0)
            {
                // The one applying failed, which the Do reports; the rest go on.
                Do(DrainAsync);
            }
        }
    }

    // ---- the queue (AM-3) ----

    /// <summary>The queued files' names, one per list item, for the bundle's find; the empty line has none.</summary>
    private IReadOnlyList<string> Names() => _queue.Paths.Select(Path.GetFileName).Select(name => name ?? string.Empty).ToList();

    /// <summary>
    /// What a start or a hand-over asks (AM-1.1, AM-3.2): a play replaces the queue (one file with its folder's media
    /// files around it, a folder's files, several paths expanded), plays, and brings the window to the front, the
    /// starting process having let it (AppMessage); an add appends and says nothing here, since axFile says it
    /// (AM-D3). A single path is remembered for Open recent.
    /// </summary>
    private async Task ApplyAsync(HandOver message)
    {
        if (message.Verb == AppMessage.Add)
        {
            var paths = await Task.Run(() => MediaFolder.Expand(message.Paths));
            _queue.Add(paths);
            FillList();
            RememberPath(message.Paths);
            return;
        }

        var (queue, start) = await Task.Run(() => MediaFolder.Queue(message.Paths));
        if (queue.Count == 0)
        {
            AxitMenu.Activate(Handle);
            Announce($"No sound or video files in {Describe(message.Paths)}", true);
            return;
        }

        _queue.Set(queue, start);
        FillList();
        _list.SelectedIndex = start;
        PlayCurrent(prompted: true);
        AxitMenu.Activate(Handle);
        RememberPath(message.Paths);
    }

    /// <summary>One path handed over, remembered for Open recent (AM-3.2); several are not, since the list would not say what they were.</summary>
    private void RememberPath(IReadOnlyList<string> paths)
    {
        if (paths.Count == 1)
        {
            _settings.Remember(paths[0]);
            SaveSettings();
        }
    }

    private static string Describe(IReadOnlyList<string> paths) => paths.Count == 1 ? PathName.Of(paths[0]) : $"{paths.Count} paths";

    /// <summary>
    /// The list's lines made again from the queue (AM-2.1): each line changed in place while the count is the same,
    /// so that the selection stays where it is and nothing but the changed line moves under NVDA; the whole list
    /// otherwise, the selection kept by index. The list is named with its count.
    /// </summary>
    private void FillList()
    {
        var lines = _queue.Lines(PlayerActive);
        _list.AccessibleName = _queue.ListName;
        if (lines.Count == _list.Items.Count)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                if (!string.Equals(_list.Items[i].ToString(), lines[i], StringComparison.Ordinal))
                {
                    _list.Items[i] = lines[i];
                }
            }

            return;
        }

        var index = _list.SelectedIndex;
        _list.BeginUpdate();
        _list.Items.Clear();
        _list.Items.AddRange(lines.Cast<object>().ToArray());
        _list.EndUpdate();
        _list.SelectedIndex = Math.Clamp(index, 0, _list.Items.Count - 1);
        UpdateStatusBar();
    }

    private bool PlayerActive => _player is { State: PlayerState.Playing or PlayerState.Paused or PlayerState.Opening };

    /// <summary>The speed shown: the player's rate once it exists (a file that refuses the rate plays at 1, AM-4.2), the setting before.</summary>
    private double Speed => _player?.Rate ?? _settings.Speed;

    /// <summary>The paths appended (Open files, Open folder, a recent item): "Added n", or why nothing was; each path remembered.</summary>
    private void AddPaths(IReadOnlyList<string> paths, string? noneText)
    {
        foreach (var path in paths)
        {
            _settings.Remember(path);
        }

        SaveSettings();
        Do(async () =>
        {
            var expanded = await Task.Run(() => MediaFolder.Expand(paths));
            if (expanded.Count == 0)
            {
                Announce(noneText ?? "No sound or video files to add", true);
                return;
            }

            _queue.Add(expanded);
            FillList();
            Announce($"Added {expanded.Count}", true);
        });
    }

    /// <summary>Queue → Open files… (Ctrl+O): the Windows file picker on the media kinds, several at once, appended in the picker's order.</summary>
    private void OpenFiles()
    {
        var extensions = string.Join(";", MediaFiles.Extensions.Order().Select(extension => "*" + extension));
        using var dialog = new OpenFileDialog
        {
            Title = "Choose sound or video files",
            Filter = $"Sound and video files ({extensions})|{extensions}|All files (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true,
        };
        if (_queue.PlayingPath is { } playing && Path.GetDirectoryName(playing) is { } folder)
        {
            dialog.InitialDirectory = folder;
        }

        var chosen = ShowSystemDialog(dialog) == DialogResult.OK ? dialog.FileNames : [];
        _list.Select();
        if (chosen.Length > 0)
        {
            AddPaths(chosen, null);
        }
    }

    /// <summary>Queue → Open folder… (Ctrl+Shift+O): the bundle's folder picker; the folder's media files appended in name order.</summary>
    private void OpenFolder()
    {
        var chosen = ChooseFolder("Choose the folder whose sound and video files to add");
        _list.Select();
        if (chosen is not null)
        {
            AddPaths([chosen], $"No sound or video files in {PathName.Of(chosen)}");
        }
    }

    /// <summary>Queue → Open recent (AM-3.2): the last ten files and folders added or handed over, those that still exist, "1. name (path)" (AX-9.5); one chosen is appended again.</summary>
    private void FillRecent() =>
        RecentMenu.Fill(_recentItem, _settings.Recent, path => File.Exists(path) || Directory.Exists(path), PathName.Of, "&No recent files or folders",
            target => AddPaths([target], Directory.Exists(target) ? $"No sound or video files in {PathName.Of(target)}" : null));

    /// <summary>Queue → New window (Ctrl+N): another axMedia window, empty; the Axit menu's New window does the same (AX-9).</summary>
    private void NewWindow() => AxitMenu.Start(Dispatch.AxMedia, null);

    /// <summary>The file in hand (AM-1.4): the one playing or paused, else the one under the selection; null with an empty queue.</summary>
    private string? FileInHand =>
        PlayerActive && _queue.PlayingPath is { } playing ? playing
        : _queue.IsFile(_list.SelectedIndex) ? _queue.Paths[_list.SelectedIndex]
        : null;

    /// <summary>The file in hand for a command; null, and said, when the queue is empty.</summary>
    private string? CurrentFile()
    {
        if (FileInHand is { } file)
        {
            return file;
        }

        Announce(PlayQueue.EmptyText, true);
        return null;
    }

    /// <summary>Queue → Show current file in → File Explorer (Ctrl+W): the file selected in File Explorer, one of the system's own windows.</summary>
    private void ShowInExplorer()
    {
        if (CurrentFile() is { } file)
        {
            OpenInExplorer(file);
        }
    }

    /// <summary>Enter in the list, Queue → Play: the file under the selection plays, "Playing <name>"; the empty line is read again.</summary>
    private void PlaySelected()
    {
        if (!_queue.Select(_list.SelectedIndex))
        {
            Announce(PlayQueue.EmptyText, true);
            return;
        }

        PlayCurrent(prompted: true);
    }

    /// <summary>Delete in the list, Queue → Remove: the file taken out, "Removed <name>"; the one playing goes on to the next, or stops at the end.</summary>
    private void RemoveSelected()
    {
        var index = _list.SelectedIndex;
        var wasPlaying = _queue.IsFile(index) && index == _queue.Playing && PlayerActive;
        if (_queue.Remove(index) is not { } removed)
        {
            Announce(PlayQueue.EmptyText, true);
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

        FillList();
        Announce($"Removed {removed}", true);
    }

    /// <summary>Queue → Clear queue: the files gone and the player stopped; "Queue cleared".</summary>
    private void ClearQueue()
    {
        _queue.Clear();
        StopPlayback();
        FillList();
        Announce("Queue cleared", true);
    }

    /// <summary>Ctrl+I, Queue → Information: the file's name and place, "03 Chapter 3.mp3, 3 of 12", with the position while it is the one playing; the empty line is read again.</summary>
    private void SayInformation()
    {
        var index = _list.SelectedIndex;
        if (!_queue.IsFile(index))
        {
            Announce(PlayQueue.EmptyText, true);
            return;
        }

        var text = _queue.Information(index);
        if (index == _queue.Playing && _player is { State: PlayerState.Playing or PlayerState.Paused } player)
        {
            text += $", {(player.State == PlayerState.Playing ? "playing" : "paused")}, {MediaFiles.FormatTime(player.Position)} of {MediaFiles.FormatTime(player.Duration)}";
        }

        Announce(text, true);
    }
}

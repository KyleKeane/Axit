using Axit.Core;
using Axit.Core.Transcript;

namespace Axit.Forms;

/// <summary>
/// The window of a console app (docs/axit/SPEC.md AX-6.5; docs/axterminal/SPEC.md AT-2): a program in a pseudo
/// console shown as a transcript with single-key navigation (<see cref="TranscriptView"/>), a multi-line field at
/// the bottom (or the control area in its place), a status bar with two labels, and everything AxClaude's window and
/// AxTerminal's window had twice until 2026-10-23 (docs/axit/review-2026-10-23.md, 5.1): the pty bridge with its
/// inbox and quiet timer, the paced write queue with one pump, the recording, the export, the find, the signals and
/// the music, the focus keys, the bookmark toggle, the notice hooks and the font. One class in partial files: this
/// one (the controls, the hooks a console window fills in, the keys, the focus, the close), <c>ConsoleForm.Process.cs</c>
/// (the program's start, output, exit and stop; the writes), <c>ConsoleForm.Files.cs</c> (find, export, recording)
/// and <c>ConsoleForm.Signals.cs</c> (the chimes, the taskbar flash, the music). AxClaude's <c>MainForm</c> keeps
/// what is Claude's and AxTerminal's <c>TerminalForm</c> what is the shell's.
/// </summary>
public abstract partial class ConsoleForm : AxitForm
{
    private const string RecordItemText = "&Record raw stream for a bug report...";

    private readonly TranscriptKeys _keys;
    private readonly string _transcriptName;
    private ToolStripMenuItem? _musicItem;
    private bool _closeConfirmed;

    /// <param name="model">The app's model on the bundle's <see cref="LineStore"/>, which the window feeds and shows; the app keeps its own typed reference.</param>
    /// <param name="keys">The window's keys for the view, from its table.</param>
    /// <param name="transcriptName">What NVDA calls the view: "Conversation", "Terminal".</param>
    /// <param name="inputName">What NVDA calls the field: "Message to Claude", "Command".</param>
    protected ConsoleForm(string appName, ConsoleSettings settings, string settingsPath, string? settingsError, LineStore model, TranscriptKeys keys, string transcriptName, string inputName)
        : base(appName, settings, settingsPath, settingsError)
    {
        ConsoleSettings = settings;
        Model = model;
        _keys = keys;
        _transcriptName = transcriptName;
        Text = appName;
        Size = new Size(1000, 700);
        MinimumSize = new Size(600, 400);

        Model.MaxLines = settings.MaxTranscriptLines;
        Model.JoinWrappedLines = settings.JoinWrappedLines;
        Model.TimeStamps = settings.MarkerTimeStamps;
        Model.Changed += OnModelChanged;

        Transcript = new TranscriptView(keys, transcriptName) { ShowMarkers = settings.ShowMarkers, Dock = DockStyle.Fill, TabIndex = 1 };
        Transcript.EscapePressed += () => BottomControl.Focus();
        Transcript.BookmarkToggled += ToggleBookmark;

        Input.AccessibleName = inputName;
        Input.PlaceholderText = inputName;
        Input.Multiline = true;
        Input.AcceptsReturn = true;
        Input.AcceptsTab = false;
        Input.WordWrap = true;
        Input.ScrollBars = ScrollBars.Vertical;
        // The control's default of 32 767 characters cut a long paste short without a word (AxClaude FR-2.1).
        Input.MaxLength = 0;
        Input.Dock = DockStyle.Bottom;
        Input.TabIndex = 0;
        // The bundle's page and paragraph keys (AxClaude FR-2.1): the control's own keys do nothing while the text fits, or scroll a row and leave the caret.
        Input.KeyDown += EditKeys.Handle;

        // What the program waits on, or a list of the window's own, takes the field's place (AxClaude D33), one control at a time.
        Area.TabIndex = 0;
        SizeChanged += (_, _) => Area.FitHeight();

        // The bundle's status bar pair (no AccessibleName, no Spring; StatusLayout keeps both inside the strip).
        (StateLabel, PlaceLabel) = StatusLayout.Pair(StatusBar);

        RecordItem = new ToolStripMenuItem(RecordItemText);
        RecordItem.Click += (_, _) => ToggleRecording();
        CommandPalette.FilledOnOpening(RecentFoldersItem).DropDownOpening += (_, _) => FillRecentFolders();
        FillRecentFolders();

        _quiet.Tick += (_, _) =>
        {
            _quiet.Stop();
            Model.EndFrame();
        };
        _exitSettle.Tick += (_, _) =>
        {
            _exitSettle.Stop();
            FinishExit();
        };
        _pump.Tick += (_, _) =>
        {
            _pump.Stop();
            PumpWrites();
        };

        // The overlay is hidden until a notice shows; it then fills the space of the hidden view and field. The
        // window adds its menu items to MenuBar after this constructor.
        Controls.Add(Overlay);
        Controls.Add(Transcript);
        Controls.Add(Input);
        Controls.Add(Area);
        Controls.Add(StatusBar);
        Controls.Add(MenuBar);
        Transcript.BringToFront();
        MainMenuStrip = MenuBar;
    }

    /// <summary>The console fields of the app's settings: the size, the recent folders, the sounds, the signals, the transcript options.</summary>
    protected ConsoleSettings ConsoleSettings { get; }

    /// <summary>The app's model, as the base sees it: the lines, the frames, the system lines, the bookmarks.</summary>
    protected LineStore Model { get; }

    /// <summary>The read-only transcript with its single-key navigation.</summary>
    protected TranscriptView Transcript { get; }

    /// <summary>The multi-line field at the bottom: the message to Claude, the command to the shell.</summary>
    protected TextBox Input { get; } = new();

    /// <summary>What takes the field's place: Claude's question, the recent commands.</summary>
    protected ControlArea Area { get; } = new();

    /// <summary>The menu bar; the window adds its menus, with <see cref="RecentFoldersItem"/>, <see cref="RecordItem"/> and the items the helpers below make among them.</summary>
    protected MenuStrip MenuBar { get; } = new();

    protected StatusStrip StatusBar { get; } = new();

    /// <summary>The left status label: the program's state.</summary>
    protected ToolStripStatusLabel StateLabel { get; }

    /// <summary>The right status label: the folder, or the shell's prompt.</summary>
    protected ToolStripStatusLabel PlaceLabel { get; }

    /// <summary>Options → Developer → Record raw stream for a bug report, or Stop recording while one runs.</summary>
    protected ToolStripMenuItem RecordItem { get; }

    /// <summary>The first menu's Open recent folder submenu, filled from the settings as it opens; a folder chosen goes to <see cref="SwitchFolder"/>.</summary>
    protected ToolStripMenuItem RecentFoldersItem { get; } = new("Open &recent folder");

    /// <summary>The control at the bottom of the window: the field, or the control area's list or field in its place.</summary>
    protected Control BottomControl => Area.Current ?? Input;

    // ---- what the console window fills in ----

    /// <summary>Whether the program is busy, for the music (AxClaude FR-7.9, AxTerminal AT-5.5): Claude working and not asking, a command running.</summary>
    protected abstract bool Working { get; }

    /// <summary>The window's own pass after the transcript changed (a frame ended, a line went in), after the view, the status bar and the music: the tick, the speech, the attention.</summary>
    protected abstract void OnTranscriptChanged();

    /// <summary>The status bar's two labels for the moment, through <see cref="SetStatus"/>.</summary>
    protected abstract void UpdateStatus();

    /// <summary>The program's process ended with <paramref name="code"/>, its last output in and the process stopped: the window's system line and announcement.</summary>
    protected abstract void OnProcessExited(int code);

    /// <summary>Alt+F4 with the program in the middle of something: true when the window asked first (a notice whose Close anyway calls <see cref="CloseConfirmed"/>), false to close now.</summary>
    protected abstract bool AskBeforeClosing();

    /// <summary>The folder the window is on, for the bundle's Recently closed list (AX-9.4): the project folder, the shell's current directory; null when none.</summary>
    protected abstract string? FolderInHand { get; }

    /// <summary>Open recent folder (AxClaude FR-8.1, AxTerminal AT-1.5): the program again in that folder.</summary>
    protected abstract void SwitchFolder(string folder);

    /// <summary>The window's own keys, from its table, after the console's shared ones (the focus keys, find, the bookmark, read to the end).</summary>
    protected virtual bool ProcessConsoleKey(Keys keyData) => false;

    // ---- the window base's hooks, filled in for both console windows ----

    /// <summary>The notice chime follows the sounds setting (AxClaude FR-7.3, AxTerminal AT-5.1).</summary>
    protected override bool NoticeChime => ConsoleSettings.SoundOnBell;

    protected override void HideForNotice()
    {
        Transcript.Visible = false;
        Input.Visible = false;
        Area.Visible = false;
    }

    /// <summary>The view, and the field or the control area, whichever was in place when the notice closes.</summary>
    protected override void ShowAfterNotice()
    {
        Transcript.Visible = true;
        Input.Visible = Area.Current is null;
        Area.Visible = Area.Current is not null;
    }

    /// <summary>The bottom of the window, whichever control is there when the notice closes and the one that had the focus is gone (an answered question's list).</summary>
    protected override Control FocusTarget => BottomControl;

    /// <summary>Everything is announced on the view, the control NVDA reads the window by.</summary>
    protected override Control AnnounceSource => Transcript;

    /// <summary>A reader keeps their line while the notice hides the view (AxClaude FR-3.3): the notice opened from the view.</summary>
    protected override void OnNoticeOpened(Control? focusBefore) => Transcript.KeepCaret = ReferenceEquals(focusBefore, Transcript);

    /// <summary>Cleared after the notice's action, so that a system line the action adds does not move a reader's caret.</summary>
    protected override void OnNoticeClosed() => Transcript.KeepCaret = false;

    protected override void ApplyFont(Font font)
    {
        Transcript.Font = font;
        Input.Font = font;
        Input.Height = TextRenderer.MeasureText("Wg", font).Height * 3 + 8;
        Area.Font = font;
        Area.MinimumHeight = Input.Height;
        Area.FitHeight();
    }

    /// <summary>The update's steps as system lines in the transcript (AxClaude FR-1.10).</summary>
    protected override void UpdateNoted(string message) => Model.AddSystemLine(message);

    /// <summary>A settings file that could not be read is a system line, not a notice over the start of the program.</summary>
    protected override void SettingsNotRead(string error) => Model.AddSystemLine("The settings file could not be read, so the defaults are in use: " + error);

    /// <summary>An error the user must see: logged, a system line in the transcript, the status, and the notice (AxClaude FR-13.1).</summary>
    protected sealed override void Fail(string message)
    {
        Log.Error(message);
        Model.AddSystemLine(message);
        UpdateStatus();
        ShowError("Error", message);
    }

    /// <summary>The focus starts in the field (AxClaude FR-6.1, AxTerminal AT-2.2); a notice shown by what follows takes it from here and gives it back.</summary>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Input.Focus();
    }

    /// <summary>
    /// Closing: the window asks first while the program is in the middle of something (AxClaude FR-1.7, AxTerminal
    /// AT-1.7), unless Windows or Close all windows closes it; then the folder joins Recently closed (AX-9.4), the
    /// process and the recording stop, and the base saves the window and starts the installed new version (AX-3.2).
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && !_closeConfirmed && AskBeforeClosing())
        {
            e.Cancel = true;
            base.OnFormClosing(e);
            return;
        }

        if (FolderInHand is { } folder)
        {
            RecentlyClosed.Remember(AppName, folder);
        }

        // A close the user confirmed while the program was busy ends it at once; Windows' own close gives it its two seconds.
        StopProcess(terminateAtOnce: _closeConfirmed);
        StopRecording(announce: false);
        base.OnFormClosing(e);
    }

    /// <summary>The Close anyway of the closing question: the window closes without asking again.</summary>
    protected void CloseConfirmed()
    {
        _closeConfirmed = true;
        Close();
    }

    // ---- keys and focus (AxClaude FR-6, AxTerminal AT-2.2) ----

    /// <summary>The console's shared keys, then the window's own (<see cref="ProcessConsoleKey"/>).</summary>
    protected sealed override bool ProcessWindowKey(Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Tab:
            case Keys.Shift | Keys.Tab:
            case BundleKeys.NextControl:
            case BundleKeys.PreviousControl:
            case BundleKeys.NextControlAlso:
                // The window has two places, the view and the control at the bottom (the field, or the control area
                // in its place): every one of these keys moves between them, so a hidden control never takes the focus.
                if (Transcript.Focused)
                {
                    BottomControl.Focus();
                }
                else
                {
                    Transcript.Focus();
                }

                return true;
            case BundleKeys.Find:
                FindDialog();
                return true;
            case BundleKeys.FindNext:
                FindNext(backward: false);
                return true;
            case BundleKeys.FindPrevious:
                FindNext(backward: true);
                return true;
        }

        if (keyData == _keys.BookmarkLine)
        {
            Transcript.ToggleBookmark();
            return true;
        }

        // Ctrl+Alt with a quick key letter reads that kind of line to the end, from anywhere (AxClaude FR-5, D36).
        if ((keyData & Keys.Modifiers) == _keys.ReadToEnd && Transcript.ReadToEnd(keyData & Keys.KeyCode))
        {
            return true;
        }

        return ProcessConsoleKey(keyData);
    }

    /// <summary>
    /// Ctrl+1, Ctrl+2 and the Navigate menu (AxClaude FR-6.5, D26): the control takes the focus, and NVDA announces the
    /// change. When it has the focus already, its name is spoken instead, so the key also answers "where am I".
    /// </summary>
    protected void GoTo(Control control)
    {
        if (control.Focused)
        {
            Announce(control.AccessibleName ?? string.Empty, true);
            return;
        }

        control.Focus();
    }

    /// <summary>The field comes back in the control area's place, with the focus when the area had it.</summary>
    protected virtual void CloseArea()
    {
        if (Area.Current is null)
        {
            return;
        }

        var focus = Area.ContainsFocus;
        Input.Visible = !NoticeOpen;
        if (focus)
        {
            Input.Focus();
        }

        Area.Clear();
    }

    /// <summary>m, Ctrl+Shift+K or Navigate → Bookmark this line (AxClaude FR-3.9): the model adds or removes the bookmark line and the result is spoken.</summary>
    private void ToggleBookmark(Line line)
    {
        if (Model.ToggleBookmark(line) is not ({ } bookmark, var added))
        {
            Announce("Line gone", true);
            return;
        }

        Announce($"{bookmark.Text} {(added ? "added" : "removed")}", true);
    }

    // ---- the transcript and the status bar ----

    /// <summary>The model changed: the view takes the lines, the status bar and the music follow, then the window's own pass.</summary>
    private void OnModelChanged()
    {
        Transcript.Sync(Model.Lines);
        UpdateStatus();
        UpdateMusic();
        OnTranscriptChanged();
    }

    /// <summary>The two status labels, fitted into the strip when either changed (AxClaude §6.2).</summary>
    protected void SetStatus(string state, string place)
    {
        if (StateLabel.Text == state && PlaceLabel.Text == place)
        {
            return;
        }

        StateLabel.Text = state;
        PlaceLabel.Text = place;
        StatusLayout.Fit(StatusBar, StateLabel, PlaceLabel);
    }

    // ---- the menu items both windows have, for the window to place ----

    /// <summary>Find…, Find next and Find previous for the Navigate menu (AxClaude FR-3.7).</summary>
    protected ToolStripItem[] FindItems() =>
    [
        Item("&Find...", FindDialog, BundleKeys.Find),
        Item("Find &next", () => FindNext(backward: false), BundleKeys.FindNext),
        Item("Find &previous", () => FindNext(backward: true), BundleKeys.FindPrevious),
    ];

    /// <summary>Bookmark this line, Next bookmark and Previous bookmark for the Navigate menu (AxClaude FR-3.9), with the view's letters named.</summary>
    protected ToolStripItem[] BookmarkItems()
    {
        var view = _transcriptName.ToLowerInvariant();
        return
        [
            new ToolStripMenuItem("&Bookmark this line", null, (_, _) => Transcript.ToggleBookmark()) { ShortcutKeyDisplayString = $"{BundleKeys.Text(_keys.BookmarkLine)}, or {BundleKeys.Text(_keys.BookmarkToggle)} in the {view}" },
            new ToolStripMenuItem("Next bookmar&k", null, (_, _) => Transcript.JumpToBookmark(backward: false)) { ShortcutKeyDisplayString = $"{BundleKeys.Text(_keys.Bookmark.Key)} in the {view}" },
            new ToolStripMenuItem("Pre&vious bookmark", null, (_, _) => Transcript.JumpToBookmark(backward: true)) { ShortcutKeyDisplayString = $"Shift+{BundleKeys.Text(_keys.Bookmark.Key)} in the {view}" },
        ];
    }

    /// <summary>The transcript's two switches for Options → Text: the time stamps on the markers and the # markers in the text (AxClaude FR-4.6, FR-4.6a).</summary>
    protected ToolStripItem[] TranscriptTextItems() =>
    [
        Toggle("Time &stamps on Input and Output lines", ConsoleSettings.MarkerTimeStamps, v =>
        {
            ConsoleSettings.MarkerTimeStamps = v;
            Model.TimeStamps = v;
        }),
        Toggle("Mar&kers in the text", ConsoleSettings.ShowMarkers, v =>
        {
            ConsoleSettings.ShowMarkers = v;
            Transcript.ShowMarkers = v;
            Transcript.Sync(Model.Lines);
        }),
    ];

    /// <summary>Options → Sounds → Background music …, made with the window's words and key; <see cref="ToggleMusic"/> flips it from anywhere.</summary>
    protected ToolStripMenuItem CreateMusicItem(string text, Keys key)
    {
        _musicItem = Toggle(text, ConsoleSettings.WorkingMusic, v =>
        {
            ConsoleSettings.WorkingMusic = v;
            UpdateMusic();
        });
        _musicItem.ShortcutKeyDisplayString = BundleKeys.Text(key);
        return _musicItem;
    }

    private void FillRecentFolders() => RecentMenu.FillFolders(RecentFoldersItem, ConsoleSettings.RecentFolders, "&No recent folders", SwitchFolder);
}

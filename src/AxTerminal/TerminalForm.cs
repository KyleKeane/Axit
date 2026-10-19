using System.Runtime.InteropServices;
using AxTerminal.Core;
using Axit.Core;
using Axit.Core.Pty;
using Axit.Forms;

namespace AxTerminal;

/// <summary>
/// AxTerminal's window (SPEC.md AT-2): AxClaude's shape around a shell. The transcript view, the command field (or
/// the recent commands list in its place), a status bar with the state and the prompt; the model turns the shell's
/// output into lines and the marks into state; the notices, the fonts, the window's place, the update flow and find
/// are the bundle's window base (<see cref="AxitForm"/>). One class in partial files, one per section.
/// </summary>
internal sealed partial class TerminalForm : AxitForm
{
    private const string RecordItemText = "&Record raw stream for a bug report...";

    /// <summary>Spoken for plain Escape in the command field (AT-3.5; AxClaude D20).</summary>
    private const string EscapeGuardText = "Shift+Escape sends Escape, Ctrl+Shift+C interrupts";

    /// <summary>How long output must have stopped before a cursor row that ends like a question is announced (AT-5.4).</summary>
    private const int AsksQuietMs = 2000;

    private readonly TerminalSettings _settings;
    private readonly string? _settingsError;
    private readonly TerminalModel _model;
    private readonly TranscriptView _transcript = new(AxTerminalKeys.Transcript, "Terminal");
    private readonly TextBox _input = new();
    private readonly ControlArea _area = new();
    private readonly MenuStrip _menu = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _state;
    private readonly ToolStripStatusLabel _promptLabel;
    private readonly ToolStripMenuItem _recentFoldersItem = new("Open &recent folder");
    private readonly ToolStripMenuItem _recordItem = new(RecordItemText);
    /// <summary>Options → Sounds → Background music while a command runs, made with the menu; Ctrl+Space flips it from anywhere (AT-5.5).</summary>
    private ToolStripMenuItem _musicItem = null!;
    private readonly List<(OutputSpeechMode Mode, ToolStripMenuItem Item)> _speechItems = [];
    private readonly List<(ShellKind Kind, ToolStripMenuItem Item)> _shellItems = [];

    /// <summary>The Speak output choices of Options → Speech in menu order (AT-5.3); the text without its mnemonic is what the choice announces.</summary>
    private static readonly (OutputSpeechMode Mode, string Text)[] SpeechChoices =
    [
        (OutputSpeechMode.None, "Speak output: &none"),
        (OutputSpeechMode.All, "Speak output: &all lines"),
    ];

    private readonly System.Windows.Forms.Timer _quiet = new() { Interval = 100 };
    private readonly System.Windows.Forms.Timer _exitSettle = new() { Interval = 300 };
    /// <summary>Looks every half second for a running program that seems to wait on a question (AT-5.4).</summary>
    private readonly System.Windows.Forms.Timer _asks = new() { Interval = 500 };
    private readonly Queue<(string Text, int DelayAfter)> _writes = new();
    /// <summary>The per-frame pass behind the tick and the spoken output (AT-5.2, AT-5.3).</summary>
    private readonly TerminalArrivals _arrivals = new();
    private readonly CommandHistory _history = new();

    private PtyHost? _host;
    private StreamRecorder? _recorder;
    private int _exitCode;
    /// <summary>The folder the shell was started in; the shell's own directory (<see cref="CurrentDirectory"/>) follows its cd.</summary>
    private string _folder;
    private ShellKind _shell;
    private string? _customShell;
    private bool _writing;
    private bool _musicOn;
    /// <summary>When output last arrived, for the question heuristic (AT-5.4).</summary>
    private long _lastOutputTick;
    /// <summary>The cursor row announced as a question, not announced again while it stays.</summary>
    private string? _askedRow;
    private string _findText = string.Empty;
    private bool _closeConfirmed;

    public TerminalForm(string? folder, TerminalSettings settings, string? settingsError)
        : base(AxTerminalApp.Name, settings, TerminalSettings.DefaultPath)
    {
        _settings = settings;
        _settingsError = settingsError;
        _folder = folder ?? (settings.LastFolder is { } last && Directory.Exists(last) ? last : Environment.CurrentDirectory);
        _shell = settings.ShellKind;
        _customShell = settings.CustomShell;
        // The bundle's status bar pair (no AccessibleName, no Spring; StatusLayout keeps both inside the strip).
        (_state, _promptLabel) = StatusLayout.Pair(_status);
        _model = new TerminalModel(settings.PtyColumns, settings.PtyRows)
        {
            MaxLines = settings.MaxTranscriptLines,
            JoinWrappedLines = settings.JoinWrappedLines,
            TimeStamps = settings.MarkerTimeStamps,
        };
        _transcript.ShowMarkers = settings.ShowMarkers;
        _model.Changed += OnModelChanged;
        _model.Ready += OnReady;
        _model.CommandFinished += OnCommandFinished;
        _quiet.Tick += (_, _) =>
        {
            _quiet.Stop();
            _model.EndFrame();
        };
        _exitSettle.Tick += (_, _) =>
        {
            _exitSettle.Stop();
            FinishExit();
        };
        _asks.Tick += (_, _) => CheckForQuestion();
        BuildUi();
        ApplyTextFont();
        RestoreWindow();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Focus first: a notice shown by what follows (the shell not found) takes the focus from here and gives it back.
        _input.Focus();
        if (_settingsError is not null)
        {
            _model.AddSystemLine("The settings file could not be read, so the defaults are in use: " + _settingsError);
        }

        StartShell();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && !_closeConfirmed && _host is not null && _model.Running)
        {
            // AT-1.7: closing now ends the command in the middle of its work. Ask first, inside the window (AX-7.2).
            e.Cancel = true;
            ShowNotice(new Notice("A command is still running",
                $"{_model.RunningCommand} is still running. Closing the window ends the shell and the command with it.",
            [
                new OverlayChoice("&Close anyway", () =>
                {
                    _closeConfirmed = true;
                    Close();
                }, IsDefault: true),
                new OverlayChoice("&Keep running", IsCancel: true),
            ]));
            base.OnFormClosing(e);
            return;
        }

        // AX-9.4: the folder joins the bundle's Recently closed list.
        RecentlyClosed.Remember(AxTerminalApp.Name, CurrentDirectory);
        StopShell();
        StopRecording(announce: false);
        // The window's place, the settings and, after Update and restart, the new version's start (AX-3.2).
        base.OnFormClosing(e);
    }

    /// <summary>The window's own keys; the text size keys, the notice's hold and F1 are the window base's.</summary>
    protected override bool ProcessWindowKey(Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Tab:
            case Keys.Shift | Keys.Tab:
            case BundleKeys.NextControl:
            case BundleKeys.PreviousControl:
            case BundleKeys.NextControlAlso:
                // Two places, the terminal and the control at the bottom (the field, or the recent commands list in
                // its place): every one of these keys moves between them, so a hidden control never takes the focus.
                if (_transcript.Focused)
                {
                    BottomControl.Focus();
                }
                else
                {
                    _transcript.Focus();
                }

                return true;
            case AxTerminalKeys.GoToField:
                GoTo(BottomControl);
                return true;
            case AxTerminalKeys.GoToTerminal:
                GoTo(_transcript);
                return true;
            case AxTerminalKeys.LatestOutput:
                _transcript.JumpToLatestResponse();
                return true;
            case AxTerminalKeys.NewSession:
                NewSession();
                return true;
            case AxTerminalKeys.RestartShell:
                RestartShell();
                return true;
            case AxTerminalKeys.ClearTranscript:
                ClearTranscript();
                return true;
            case AxTerminalKeys.RecentCommands:
                ShowRecentCommands();
                return true;
            case AxTerminalKeys.WhatIsRunning:
                SayWhatIsRunning();
                return true;
            case AxTerminalKeys.SayFolder:
                SayFolder();
                return true;
            case AxTerminalKeys.ToggleMusic:
                ToggleMusic();
                return true;
            case AxTerminalKeys.BookmarkLine:
                _transcript.ToggleBookmark();
                return true;
            case AxTerminalKeys.ShowInExplorer:
                ShowInExplorer();
                return true;
            case AxTerminalKeys.Interrupt:
                Interrupt();
                return true;
            case AxTerminalKeys.SendEscape:
                SendEscape();
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
            case BundleKeys.Save:
                ExportTranscript();
                return true;
        }

        // Ctrl+Alt with a quick key letter reads that kind of line to the end, from anywhere (AxClaude D36).
        if ((keyData & Keys.Modifiers) == AxTerminalKeys.ReadToEnd && _transcript.ReadToEnd(keyData & Keys.KeyCode))
        {
            return true;
        }

        return false;
    }

    // ---- what the window base asks for (bundle AX-6.5) ----

    protected override string Shortcuts => HelpText.Shortcuts;

    /// <summary>The notice chime follows the sounds setting (AT-5.1).</summary>
    protected override bool NoticeChime => _settings.SoundOnBell;

    protected override void HideForNotice()
    {
        _transcript.Visible = false;
        _input.Visible = false;
        _area.Visible = false;
    }

    /// <summary>The terminal, and the field or the recent commands list, whichever was in place.</summary>
    protected override void ShowAfterNotice()
    {
        _transcript.Visible = true;
        _input.Visible = _area.Current is null;
        _area.Visible = _area.Current is not null;
    }

    protected override Control FocusTarget => BottomControl;

    /// <summary>The terminal keeps the focus across a notice when it had it; the bottom of the window is found afresh.</summary>
    protected override Control? FocusToKeep() => _transcript.Focused ? _transcript : null;

    /// <summary>A reader keeps their line while the notice hides the terminal (AxClaude FR-3.3).</summary>
    protected override void OnNoticeOpened(Control? focusBefore) => _transcript.KeepCaret = focusBefore is not null;

    protected override void OnNoticeClosed() => _transcript.KeepCaret = false;

    protected override void ApplyFont(Font font)
    {
        _transcript.Font = font;
        _input.Font = font;
        _input.Height = TextRenderer.MeasureText("Wg", font).Height * 3 + 8;
        _area.Font = font;
        _area.MinimumHeight = _input.Height;
        _area.FitHeight();
    }

    /// <summary>How the new version starts after Update and restart: a shell in the current directory.</summary>
    protected override IReadOnlyList<string> RestartArguments() => [Dispatch.AxTerminal, CurrentDirectory];

    protected override string UpdateRestartText => "closes AxTerminal, installs it and starts it again with a new shell in the same folder";

    /// <summary>The update's steps as system lines in the terminal.</summary>
    protected override void UpdateNoted(string message) => _model.AddSystemLine(message);

    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Shell: {ShellLauncher.Describe(_shell)} ({ShellLauncher.BuildCommandLine(_shell, _customShell) ?? "(not found)"})",
        $"Folder: {_folder}; current directory: {CurrentDirectory}",
        $"Console size: {_model.Columns}x{_model.Rows}",
        $"State: {_state.Text}; prompt: {_model.Prompt ?? "(none yet)"}; marks: {_model.HasMarks}",
    ];

    /// <summary>The control at the bottom of the window: the command field, or the recent commands list in its place.</summary>
    private Control BottomControl => _area.Current ?? _input;

    /// <summary>The shell's current directory as its 9;9 mark reported it, or the folder it started in.</summary>
    private string CurrentDirectory => _model.Directory ?? _folder;

    // ---- taskbar flash ----

    private const uint FLASHW_ALL = 3;
    private const uint FLASHW_TIMERNOFG = 12;

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    [DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);
}

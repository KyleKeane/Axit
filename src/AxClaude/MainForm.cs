using System.Runtime.InteropServices;
using AxClaude.Core;
using AxClaude.Core.Pty;
using AxClaude.Core.Transcript;
using Axit.Core;
using Axit.Forms;
using Axit.Core.Pty;

namespace AxClaude;

internal sealed partial class MainForm : AxitForm
{
    private const string RecordItemText = "&Record raw stream for a bug report...";

    /// <summary>Spoken for Ctrl+O, which the app does not pass on (D14).</summary>
    private const string CtrlOGuardText = "Ctrl+O is off. For tool output, start with --verbose";

    private readonly StartupOptions _options;
    private readonly AppSettings _settings;
    private readonly string? _settingsError;
    private readonly SessionModel _model;
    private readonly TranscriptView _transcript = new();
    private readonly TextBox _input = new();
    private readonly ControlArea _area = new();
    private readonly MenuStrip _menu = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _state;
    private readonly ToolStripStatusLabel _folderLabel;
    private readonly ToolStripMenuItem _recentFoldersItem = new("Open &recent folder");
    private readonly ToolStripMenuItem _recordItem = new(RecordItemText);
    /// <summary>Options → Sounds → Background music while Claude works, made with the menu; Ctrl+Space flips it from anywhere (FR-7.9).</summary>
    private ToolStripMenuItem _musicItem = null!;
    private readonly System.Windows.Forms.Timer _quiet = new() { Interval = 100 };
    private readonly System.Windows.Forms.Timer _attention = new() { Interval = 400 };
    private readonly System.Windows.Forms.Timer _exitSettle = new() { Interval = 300 };
    /// <summary>The working music is playing (FR-7.9).</summary>
    private bool _musicOn;

    /// <summary>How long reply speech waits for Claude's prompt row, which comes after a question's rows (D33).</summary>
    private readonly System.Windows.Forms.Timer _speechHold = new() { Interval = 250 };

    /// <summary>Reply speech waiting for <see cref="_speechHold"/>; dropped when it turns out to be a question's rows.</summary>
    private string? _heldSpeech;
    private readonly Queue<(string Text, int DelayAfter)> _writes = new();
    /// <summary>The per-frame pass behind the tick and the spoken replies (FR-7.8, FR-7.4).</summary>
    private readonly Arrivals _arrivals = new();
    private readonly List<(ReplySpeechMode Mode, ToolStripMenuItem Item)> _replySpeechItems = [];

    /// <summary>The Speak replies choices of Options → Speech in menu order (FR-7.4); the text without its mnemonic is what the choice announces.</summary>
    private static readonly (ReplySpeechMode Mode, string Text)[] ReplySpeechChoices =
    [
        (ReplySpeechMode.None, "Speak replies: &none"),
        (ReplySpeechMode.FirstLines, "Speak replies: &first lines"),
        (ReplySpeechMode.All, "Speak replies: &all lines"),
    ];
    private PtyHost? _host;
    private StreamRecorder? _recorder;
    private int _exitCode;
    private string? _folder;
    private bool _writing;
    private bool _bellPending;
    private bool _promptAnnounced;

    /// <summary>The signature of the question or screen the control area shows (D33); null while the message field is in its place.</summary>
    private string? _shownWait;

    /// <summary>What Ctrl+Q speaks while the control area shows (D33): the title and every line of the question or screen, uncut.</summary>
    private string? _areaText;

    /// <summary>The signature of the question or screen that was shown in the control area or announced: it is not shown again while Claude waits on it.</summary>
    private string? _handledWait;

    /// <summary>Whether a feedback draft Claude queued in the turn that just ended waits (D33): the mode line's count against the count before the turn.</summary>
    private readonly FeedbackDraftWatch _drafts = new();

    /// <summary>/feedback is about to open for a draft, once the screen has settled.</summary>
    private bool _openingDrafts;

    /// <summary>When /feedback was last sent for a draft: the list that follows within <see cref="UserActionMs"/> is the app's doing.</summary>
    private long _draftOpenedAt;

    /// <summary>The title of the draft /feedback was opened for: Claude's list of drafts is answered with it, so that the review comes straight up. Null when the list is the user's own.</summary>
    private string? _draftToReview;

    /// <summary>The review of a draft the app opened is up: the list of drafts Claude shows after Send, Discard or Later is closed for the user, back to the message field.</summary>
    private bool _reviewing;

    private const int DraftSettleMs = 500;

    /// <summary>When a key was last sent from a screen of Claude's: the screen that follows is the user's doing and comes without the chime.</summary>
    private long _screenKeyAt;

    /// <summary>When an answer was last sent: the question that follows is the user's doing and opens without the chime.</summary>
    private long _answerAt;

    /// <summary>
    /// How long after the user's answer or key what Claude shows next counts as the user's doing: no chime, no
    /// "Question closed", and the control area holds its keys that long so that a second Escape does not reach Claude.
    /// </summary>
    private const int UserActionMs = 3000;

    /// <summary>D33: the interrupting speech of a question waits this long after the focus moved, so that NVDA's reading of the new control does not cut it off.</summary>
    private const int FocusSettleMs = 300;

    /// <summary>Whether the user's answer or key at <paramref name="at"/> came less than <see cref="UserActionMs"/> ago: what follows it is prompted, not a surprise.</summary>
    private static bool UserActed(long at) => Environment.TickCount64 - at < UserActionMs;

    /// <summary>The /config setting last chosen, where the list of settings starts when Claude's list comes back.</summary>
    private string? _settingKey;

    /// <summary>The question the control area showed last, which a follow-up for the user's own words repeats.</summary>
    private Question? _askedQuestion;

    /// <summary>The slash command sent last, which the question that follows belongs to (QuestionRouter); null after a message.</summary>
    private SlashCommand? _lastCommand;

    /// <summary>A message kept out of a waiting question by the guard (FR-2.10, D33), sent after all when Enter is pressed again with the same text.</summary>
    private string? _keptMessage;
    private bool _readyAnnounced;
    private bool _messageSent;
    /// <summary>Claude's arguments after --ax-screen-reader, from the command line or the New session notice (FR-8.6); null is the default, --continue (FR-9.1). Restart keeps them.</summary>
    private List<string>? _claudeArgs;
    /// <summary>Claude found no conversation to continue in this folder: the default is no arguments until the folder changes (FR-9.1).</summary>
    private bool _nothingToContinue;
    /// <summary>The folder Claude was last started in: a restart there with --continue replays what the window shows (FR-4.8).</summary>
    private string? _launchedFolder;
    private string _findText = string.Empty;
    private bool _closeConfirmed;

    public MainForm(StartupOptions options, AppSettings settings, string? settingsError)
        : base("AxClaude", settings, AppSettings.DefaultPath)
    {
        _options = options;
        _claudeArgs = options.ClaudeArgs is { } given ? [.. given] : null;
        _settings = settings;
        _settingsError = settingsError;
        // The bundle's status bar pair (no AccessibleName, no Spring; StatusLayout keeps both inside the strip).
        (_state, _folderLabel) = StatusLayout.Pair(_status);
        _folder = options.Folder ?? (settings.LastProjectFolder is { } last && Directory.Exists(last) ? last : null);
        _model = new SessionModel(options.Columns ?? settings.PtyColumns, options.Rows ?? settings.PtyRows)
        {
            MaxLines = settings.MaxTranscriptLines,
            JoinWrappedLines = settings.JoinWrappedLines,
            TimeStamps = settings.MarkerTimeStamps,
            DetectScreens = settings.QuestionNotices,
        };
        _model.Changed += OnModelChanged;
        _model.ResponseStarted += () =>
        {
            Announce("Responding", false);
            if (_settings.SoundOnBell)
            {
                Sounds.Responding();
            }
        };
        _model.Announcement += text => Announce(text, false);
        _model.Bell += () =>
        {
            _bellPending = true;
            _attention.Stop();
            _attention.Start();
        };
        _quiet.Tick += (_, _) =>
        {
            _quiet.Stop();
            _model.EndFrame();
        };
        _attention.Tick += (_, _) =>
        {
            _attention.Stop();
            AnnounceAttention();
        };
        _exitSettle.Tick += (_, _) =>
        {
            _exitSettle.Stop();
            FinishExit();
        };
        _speechHold.Tick += (_, _) =>
        {
            _speechHold.Stop();
            if (_heldSpeech is { } speech && !QuestionShown)
            {
                Announce(speech, false);
            }

            _heldSpeech = null;
        };
        BuildUi();
        ApplyTextFont();
        RestoreWindow();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Focus first: a notice shown by what follows (Claude not found) takes the focus from here and gives it back.
        _input.Focus();
        if (_settingsError is not null)
        {
            _model.AddSystemLine("The settings file could not be read, so the defaults are in use: " + _settingsError);
        }

        if (_options.RecordPath is { } record)
        {
            StartRecording(record);
        }

        if (_folder is null && PickFolder(null) is { } chosen)
        {
            SetFolder(chosen);
        }

        if (_folder is null)
        {
            _model.AddSystemLine("No project folder. Press Ctrl+N to choose one.");
        }
        else
        {
            StartClaude();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && !_closeConfirmed && (ClaudeBusy || ClaudeBackground is not null))
        {
            // FR-1.7: closing now would cut Claude off in the middle of a turn, or stop the work it runs in the
            // background after the turn. Ask first, inside the window (D23). A Windows shutdown or Task Manager
            // closes without asking.
            e.Cancel = true;
            ConfirmClose();
            base.OnFormClosing(e);
            return;
        }

        if (_folder is { } folder)
        {
            // AX-9.4: the folder joins the bundle's Recently closed list.
            RecentlyClosed.Remember("AxClaude", folder);
        }

        StopClaude();
        StopRecording(announce: false);
        // The window's place, the settings and, after Update and restart, the new version's start (AX-3.2).
        base.OnFormClosing(e);
    }

    /// <summary>Claude is in the middle of a turn: working, waiting for an answer, or holding a message sent while it worked.</summary>
    private bool ClaudeBusy => _host is not null && (_model.Working || _model.PromptPending || _model.QueuedMessages > 0);

    /// <summary>The work Claude runs in the background, which outlives the turn ("1 shell"); null when none runs or Claude is stopped.</summary>
    private string? ClaudeBackground => _host is null ? null : _model.Background;

    /// <summary>The window's own keys; the text size keys, the notice's hold and F1 are the window base's.</summary>
    protected override bool ProcessWindowKey(Keys keyData)
    {
        // Most keys with a menu item of their own are that item's ShortcutKeys and reach the menu through the base
        // call. This switch holds the rest: the focus keys, the Ctrl+O guard, the interrupt (WinForms takes no
        // Shift-only shortcut), and the bundle and go-to keys, whose menu items only show the key.
        switch (keyData)
        {
            case Keys.Tab:
            case Keys.Shift | Keys.Tab:
            case BundleKeys.NextControl:
            case BundleKeys.PreviousControl:
            case BundleKeys.NextControlAlso:
                // The window has two places, the conversation and the control at the bottom (the message field, or
                // the control area in its place): every one of these keys moves between them, so a hidden control
                // never takes the focus.
                if (_transcript.Focused)
                {
                    BottomControl.Focus();
                }
                else
                {
                    _transcript.Focus();
                }

                return true;
            case AxClaudeKeys.GoToField:
                GoTo(BottomControl);
                return true;
            case AxClaudeKeys.ToggleMusic:
                ToggleMusic();
                return true;
            case AxClaudeKeys.ReadQuestion:
                ReadQuestionAgain();
                return true;
            case AxClaudeKeys.GoToConversation:
                GoTo(_transcript);
                return true;
            case AxClaudeKeys.CtrlOGuard:
                // D14: Claude's detailed view redraws the conversation in screen reader mode, which doubles the
                // transcript and speaks old replies again. The key is sent only to close the view should it be open.
                if (_model.TranscriptViewOpen)
                {
                    Write("\x0f");
                }
                else
                {
                    Announce(CtrlOGuardText, true);
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
            case BundleKeys.Save:
                ExportConversation();
                return true;
            case AxClaudeKeys.Interrupt:
                // The interrupt key (FR-2.3). Ctrl+Escape opens the Start menu and never reaches the app.
                Interrupt();
                return true;
        }

        // Ctrl+Alt with a quick key letter reads that kind of line to the end, from anywhere (FR-5, D36).
        if ((keyData & Keys.Modifiers) == AxClaudeKeys.ReadToEnd && _transcript.ReadToEnd(keyData & Keys.KeyCode))
        {
            return true;
        }

        return false;
    }

    // ---- what the window base asks for (bundle AX-6.5) ----

    protected override string Shortcuts => HelpText.Shortcuts;

    /// <summary>The notice chime follows the sounds setting (FR-7.3).</summary>
    protected override bool NoticeChime => _settings.SoundOnBell;

    protected override void HideForNotice()
    {
        _transcript.Visible = false;
        _input.Visible = false;
        _area.Visible = false;
    }

    /// <summary>The conversation, and the message field or the control area when Claude asked something while the notice showed.</summary>
    protected override void ShowAfterNotice()
    {
        _transcript.Visible = true;
        _input.Visible = _area.Current is null;
        _area.Visible = _area.Current is not null;
    }

    /// <summary>The bottom of the window, whichever control is there when the notice closes.</summary>
    protected override Control FocusTarget => BottomControl;

    /// <summary>The conversation keeps the focus across a notice when it had it; the bottom of the window is found afresh.</summary>
    protected override Control? FocusToKeep() => _transcript.Focused ? _transcript : null;

    /// <summary>A reader keeps their line while the notice hides the conversation (FR-3.3).</summary>
    protected override void OnNoticeOpened(Control? focusBefore) => _transcript.KeepCaret = focusBefore is not null;

    /// <summary>Cleared after the notice's action, so that a system line the action adds does not move a reader's caret.</summary>
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

    /// <summary>How the new version starts after Update and restart: on this folder, and on the conversation when a message was sent (FR-9.1).</summary>
    protected override IReadOnlyList<string> RestartArguments()
    {
        var arguments = new List<string> { "claude" };
        if (_folder is not null)
        {
            arguments.Add(_folder);
            if (_messageSent)
            {
                arguments.Add("--");
                arguments.Add("--continue");
            }
        }

        return arguments;
    }

    protected override string UpdateRestartText =>
        "closes AxClaude, installs it and starts it again on the same folder; the conversation is picked up again if one is open";

    /// <summary>The update's steps as system lines in the conversation (FR-1.10).</summary>
    protected override void UpdateNoted(string message) => _model.AddSystemLine(message);

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

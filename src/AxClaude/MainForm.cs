using AxClaude.Core;
using AxClaude.Core.Pty;
using AxClaude.Core.Transcript;
using Axit.Core;
using Axit.Core.Transcript;
using Axit.Forms;

namespace AxClaude;

/// <summary>
/// axClaude's window (SPEC.md §6): the bundle's console window (<see cref="ConsoleForm"/>: the conversation, the
/// message field, the control area, the status bar, the pty bridge, the paced writes, the recording, the export,
/// find, the signals and the music) around Claude Code in screen reader mode. What is Claude's stays here: the menus,
/// the attention and the question flow in the control area (D33), the project folder, New session, the restart with
/// --continue. One class in partial files, one per section (see src/AxClaude/CLAUDE.md).
/// </summary>
internal sealed partial class MainForm : ConsoleForm
{
    /// <summary>Spoken for Ctrl+O, which the app does not pass on (D14).</summary>
    private const string CtrlOGuardText = "Ctrl+O is off. For tool output, start with --verbose";

    private readonly StartupOptions _options;
    private readonly AppSettings _settings;
    private readonly SessionModel _model;

    /// <summary>The settle after which what Claude waits on is acted on (FR-7.2, D33): started by the bell and by a change of what waits; <see cref="OnFrameSettled"/> on its tick.</summary>
    private readonly System.Windows.Forms.Timer _attention = new() { Interval = SettleMs };

    /// <summary>Reply speech waits this long for Claude's prompt row, which comes after a question's rows (D33); <see cref="SpeakHeld"/> on its tick.</summary>
    private readonly System.Windows.Forms.Timer _speechHold = new() { Interval = SpeechHoldMs };

    /// <summary>The reply lines waiting for <see cref="_speechHold"/>; a question's rows among them are dropped once Claude turns out to be waiting, the rest is spoken.</summary>
    private readonly List<Line> _heldLines = [];

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

    private string? _folder;

    /// <summary>The bell rang (FR-7.2): "Done" or "Claude asks" once the screen has settled.</summary>
    private bool _bellPending;

    /// <summary>Whether a feedback draft Claude queued in the turn that just ended waits (D33): the mode line's count against the count before the turn.</summary>
    private readonly FeedbackDraftWatch _drafts = new();

    /// <summary>The /config setting last chosen, where the list of settings starts when Claude's list comes back.</summary>
    private string? _settingKey;

    /// <summary>The question Claude asked last (shown or not), which the text prompt after Other names and whose step says what comes next.</summary>
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

    public MainForm(StartupOptions options, AppSettings settings, string? settingsError)
        : this(options, settings, settingsError, new SessionModel(options.Columns ?? settings.PtyColumns, options.Rows ?? settings.PtyRows))
    {
    }

    /// <summary>The model is made first so that the base and this class both hold it: the base as a LineStore, this class as Claude's.</summary>
    private MainForm(StartupOptions options, AppSettings settings, string? settingsError, SessionModel model)
        : base("axClaude", settings, AppSettings.DefaultPath, settingsError, model, AxClaudeKeys.Transcript, "Conversation", "Message to Claude")
    {
        _options = options;
        _claudeArgs = options.ClaudeArgs is { } given ? [.. given] : null;
        _settings = settings;
        _model = model;
        _model.DetectScreens = settings.QuestionNotices;
        _folder = options.Folder ?? (settings.LastProjectFolder is { } last && Directory.Exists(last) ? last : null);
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
        _attention.Tick += (_, _) =>
        {
            _attention.Stop();
            OnFrameSettled();
        };
        _speechHold.Tick += (_, _) => SpeakHeld();
        Input.KeyDown += OnInputKeyDown;
        Transcript.LineChosen += QuoteLineInMessage;
        BuildMenus();
        SetStatus("Claude: starting", string.Empty);
        UpdateFolderUi();
        ApplyTextFont();
        RestoreWindow();
    }

    protected override void OnShown(EventArgs e)
    {
        // The base puts the focus in the field first: a notice shown by what follows (Claude not found) takes it from there and gives it back.
        base.OnShown(e);
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

    /// <summary>FR-1.7: closing now would cut Claude off in the middle of a turn, or stop the work it runs in the background after the turn: the question first, inside the window (D23).</summary>
    protected override bool AskBeforeClosing()
    {
        if (!ClaudeBusy && ClaudeBackground is null)
        {
            return false;
        }

        ConfirmClose();
        return true;
    }

    /// <summary>The Close axClaude? notice (FR-1.7): what Claude is doing, what closing does to it. Enter closes, Escape keeps the window.</summary>
    private void ConfirmClose()
    {
        var background = ClaudeBackground;
        string state;
        if (ClaudeBusy)
        {
            state = _model.PromptPending ? "Claude is waiting for your answer" : "Claude is still working";
            if (_model.QueuedMessages > 0)
            {
                state += _model.QueuedMessages == 1
                    ? " and a message you sent is still waiting for it"
                    : $" and {_model.QueuedMessages} messages you sent are still waiting for it";
            }

            if (background is not null)
            {
                state += $", and it has work running in the background ({background})";
            }
        }
        else
        {
            state = $"Claude has work running in the background ({background})";
        }

        var stops = ClaudeBusy ? "Claude stops in the middle of its work" : "the background work stops";
        var consequence = !RestartPending
            ? $"If you close now, {stops}."
            : $"If you close now, {stops} and the updated axClaude starts again.";
        ShowNotice(new Notice("Close axClaude?",
            $"{state}. {consequence}\n" +
            "What is done so far is saved. Start axClaude again to carry on.",
        [
            new OverlayChoice("&Close anyway", CloseConfirmed, IsDefault: true),
            new OverlayChoice("&Keep working", KeepWorkingAfterUpdate, IsCancel: true),
        ]));
    }

    /// <summary>Claude is in the middle of a turn: working, waiting for an answer, or holding a message sent while it worked.</summary>
    private bool ClaudeBusy => ProcessRunning && (_model.Working || _model.PromptPending || _model.QueuedMessages > 0);

    /// <summary>The work Claude runs in the background, which outlives the turn ("1 shell"); null when none runs or Claude is stopped.</summary>
    private string? ClaudeBackground => ProcessRunning ? _model.Background : null;

    /// <summary>The window's own keys, after the console's shared ones (the focus keys, find, the bookmark, read to the end) and the base's (the text size keys, F1, the palette, Ctrl+S).</summary>
    protected override bool ProcessConsoleKey(Keys keyData)
    {
        // Most keys with a menu item of their own are that item's ShortcutKeys and reach the menu through the base
        // call. This switch holds the rest: the focus keys, the Ctrl+O guard, the interrupt (WinForms takes no
        // Shift-only shortcut), and the keys whose menu items only show the key.
        switch (keyData)
        {
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
                GoTo(Transcript);
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
            case AxClaudeKeys.Interrupt:
                // The interrupt key (FR-2.3). Ctrl+Escape opens the Start menu and never reaches the app.
                Interrupt();
                return true;
        }

        return false;
    }

    // ---- what the window base and the console window ask for (bundle AX-6.5) ----

    protected override string Shortcuts => HelpText.Shortcuts;

    /// <summary>The music plays while Claude works and is not asking (FR-7.9).</summary>
    protected override bool Working => _model.Working && !_model.PromptPending;

    /// <summary>Ctrl+S, the bundle's save key: the conversation exported as text (FR-3.8), since the window holds no document of its own.</summary>
    protected override void SaveCommand() => ExportConversation();

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
        "closes axClaude, installs it and starts it again on the same folder; the conversation is picked up again if one is open";

    /// <summary>FR-13.3: the program, Claude, the folder, the console size and the state, after the base's version line and before its paths.</summary>
    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Program: {Environment.ProcessPath ?? "(unknown)"} (process {Environment.ProcessId})",
        $"Claude: {ClaudeLauncher.Find(_options.ClaudePath ?? _settings.ClaudePath) ?? "(not found)"}",
        $"Claude Code version: {_claudeVersion ?? "(not asked)"}",
        $"Project folder: {_folder ?? "(none)"}",
        $"Console size: {_model.Columns}x{_model.Rows}",
        $"Claude state: {StateLabel.Text}",
    ];
}

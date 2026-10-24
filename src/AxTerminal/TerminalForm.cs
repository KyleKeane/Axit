using AxTerminal.Core;
using Axit.Core;
using Axit.Forms;

namespace AxTerminal;

/// <summary>
/// axTerminal's window (SPEC.md AT-2): the bundle's console window (<see cref="ConsoleForm"/>: the terminal view, the
/// command field, the control area, the status bar, the pty bridge, the paced writes, the recording, the export,
/// find, the signals and the music) around a shell. What is the shell's stays here: the menus, the marks' state and
/// its announcements, the recent commands, the shells and their folders, the "a program asks" heuristic. One class
/// in partial files, one per section (see src/AxTerminal/CLAUDE.md).
/// </summary>
internal sealed partial class TerminalForm : ConsoleForm
{
    /// <summary>Spoken for plain Escape in the command field (AT-3.5; axClaude D20).</summary>
    private const string EscapeGuardText = "Shift+Escape sends Escape, Ctrl+Shift+C interrupts";

    /// <summary>How long output must have stopped before a cursor row that ends like a question is announced (AT-5.4).</summary>
    private const int AsksQuietMs = 2000;

    private readonly TerminalSettings _settings;
    private readonly TerminalModel _model;
    private readonly List<(OutputSpeechMode Mode, ToolStripMenuItem Item)> _speechItems = [];
    private readonly List<(ShellKind Kind, ToolStripMenuItem Item)> _shellItems = [];

    /// <summary>
    /// The shells this computer has (AT-1.2a), looked up once off the window's thread as it starts and again after an
    /// install: the menus read this and never search the PATH as they open, since a network folder on it could hold
    /// them for the network's timeout (2026-10-24 review). Empty until the first look ends: every shell shown meanwhile.
    /// </summary>
    private volatile IReadOnlySet<ShellKind>? _shellsThere;

    /// <summary>Whether the shell is there, as last looked up; true before the first look has ended.</summary>
    private bool ShellThere(ShellKind kind) => _shellsThere is not { } there || there.Contains(kind);

    /// <summary>Looks the shells up again, off the window's thread.</summary>
    private void LookUpShells() =>
        Task.Run(() => _shellsThere = Enum.GetValues<ShellKind>().Where(kind => kind != ShellKind.Custom && ShellLauncher.Find(kind) is not null).ToHashSet());

    /// <summary>The Speak output choices of Options → Speech in menu order (AT-5.3); the text without its mnemonic is what the choice announces.</summary>
    private static readonly (OutputSpeechMode Mode, string Text)[] SpeechChoices =
    [
        (OutputSpeechMode.None, "Speak output: &none"),
        (OutputSpeechMode.All, "Speak output: &all lines"),
    ];

    /// <summary>Looks every half second for a running program that seems to wait on a question (AT-5.4).</summary>
    private readonly System.Windows.Forms.Timer _asks = new() { Interval = 500 };

    /// <summary>The per-frame pass behind the tick and the spoken output (AT-5.2, AT-5.3).</summary>
    private readonly TerminalArrivals _arrivals = new();
    private readonly CommandHistory _history = new();

    /// <summary>The folder the shell was started in; the shell's own directory (<see cref="CurrentDirectory"/>) follows its cd.</summary>
    private string _folder;
    private ShellKind _shell;
    private string? _customShell;

    /// <summary>The cursor row announced as a question, not announced again while it stays.</summary>
    private string? _askedRow;

    public TerminalForm(string? folder, TerminalSettings settings, string? settingsError)
        : this(folder, settings, settingsError, new TerminalModel(settings.PtyColumns, settings.PtyRows))
    {
    }

    /// <summary>The model is made first so that the base and this class both hold it: the base as a LineStore, this class as the shell's.</summary>
    private TerminalForm(string? folder, TerminalSettings settings, string? settingsError, TerminalModel model)
        : base(AxTerminalApp.Name, settings, TerminalSettings.DefaultPath, settingsError, model, AxTerminalKeys.Transcript, "Terminal", "Command")
    {
        _settings = settings;
        _model = model;
        _folder = folder ?? (settings.LastFolder is { } last && Directory.Exists(last) ? last : Environment.CurrentDirectory);
        _shell = settings.ShellKind;
        _customShell = settings.CustomShell;
        LookUpShells();
        _model.Ready += OnReady;
        _model.CommandFinished += OnCommandFinished;
        _asks.Tick += (_, _) => CheckForQuestion();
        // The field's own keys first, the bundle's edit keys from there (Ctrl+Up and Ctrl+Down are the history, AT-3.9).
        Input.KeyDown -= EditKeys.Handle;
        Input.KeyDown += OnInputKeyDown;
        Transcript.LineChosen += CopyLineToField;
        BuildMenus();
        SetStatus("Starting", CurrentDirectory);
        UpdateTitle();
        ApplyTextFont();
        RestoreWindow();
    }

    protected override void OnShown(EventArgs e)
    {
        // The base puts the focus in the field first: a notice shown by what follows (the shell not found) takes it from there and gives it back.
        base.OnShown(e);
        StartShell();
    }

    /// <summary>AT-1.7: closing now ends the command in the middle of its work: the question first, inside the window (AX-7.2).</summary>
    protected override bool AskBeforeClosing()
    {
        if (!ProcessRunning || !_model.Running)
        {
            return false;
        }

        ShowNotice(new Notice("A command is still running",
            $"{_model.RunningCommand} is still running. Closing the window ends the shell and the command with it.",
        [
            new OverlayChoice("&Close anyway", CloseConfirmed, IsDefault: true),
            new OverlayChoice("&Keep running", IsCancel: true),
        ]));
        return true;
    }

    // ---- what the window base and the console window ask for (bundle AX-6.5) ----

    protected override string Shortcuts => HelpText.Shortcuts;

    /// <summary>The music plays while a command runs (AT-5.5).</summary>
    protected override bool Working => _model.Running;

    /// <summary>Ctrl+S, the bundle's save key: the transcript exported (AT-4.7), since the window holds no document of its own.</summary>
    protected override void SaveCommand() => ExportTranscript();

    /// <summary>Ctrl+S, Session → Export transcript…: the transcript as the reader sees it to a UTF-8 Markdown file in the current directory (AT-4.7).</summary>
    private void ExportTranscript() =>
        Export("transcript", "Export the transcript", "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            $"axterminal-{DateTime.Now:yyyy-MM-dd-HHmm}.md", "md",
            Directory.Exists(CurrentDirectory) ? CurrentDirectory : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

    /// <summary>How the new version starts after Update and restart: a shell in the current directory.</summary>
    protected override IReadOnlyList<string> RestartArguments() => [Dispatch.AxTerminal, CurrentDirectory];

    protected override string UpdateRestartText => "closes axTerminal, installs it and starts it again with a new shell in the same folder";

    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Shell: {ShellLauncher.Describe(_shell)} ({ShellLauncher.BuildCommandLine(_shell, _customShell) ?? "(not found)"})",
        $"Folder: {_folder}; current directory: {CurrentDirectory}",
        $"Console size: {_model.Columns}x{_model.Rows}",
        $"State: {StateLabel.Text}; prompt: {_model.Prompt ?? "(none yet)"}; marks: {_model.HasMarks}",
    ];

    /// <summary>The shell's current directory, for the bundle's Recently closed list (AX-9.4).</summary>
    protected override string? PathInHand => CurrentDirectory;

    protected override string Verb => Dispatch.AxTerminal;

    /// <summary>The shell's current directory as its 9;9 mark reported it, or the folder it started in.</summary>
    private string CurrentDirectory => _model.Directory ?? _folder;
}

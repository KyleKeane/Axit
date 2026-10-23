using System.Diagnostics;
using System.Text.RegularExpressions;
using AxGit.Core;
using Axit.Core;
using Axit.Forms;

namespace AxGit;

/// <summary>
/// AxGit's window (SPEC.md AG-2): a menu bar, one list with three views (Changes, Log, Branches), one read-only text
/// for the details (a diff, a commit, what git answered), and a status bar with the branch. Every action is one git
/// command run on a background task (AG-D6); the result is one announcement, or a notice with git's whole answer.
/// The notices, the fonts, the window's place, the update flow and find are the bundle's window base
/// (<see cref="AxitForm"/>).
/// </summary>
public sealed partial class GitForm : AxitForm
{
    private enum View
    {
        Changes,
        Log,
        Branches,
    }

    private const int UntrackedLimit = 200 * 1024;

    private readonly GitSettings _settings;
    private readonly string? _startPath;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _recentItem = new("Open &recent repository");
    private readonly ListBox _list = new();
    private readonly TextBox _details = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _stateLabel;
    private readonly ToolStripStatusLabel _pathLabel;

    /// <summary>The menu items that run git, disabled while a command runs and when git is missing (AG-5.4).</summary>
    private readonly List<ToolStripMenuItem> _gitItems = [];

    /// <summary>The item selected in each view, by path, hash or name, kept while the window is open (AG-2.2).</summary>
    private readonly Dictionary<View, string?> _selected = [];

    private string? _root;
    private View _view = View.Changes;
    private RepositoryStatus _repo = RepositoryStatus.Empty;
    private IReadOnlyList<Commit> _commits = [];
    private IReadOnlyList<Branch> _branches = [];
    private bool _logLoaded;
    private bool _branchesLoaded;

    /// <summary>The verb of the git command running now, for the status bar; null between commands.</summary>
    private string? _busy;

    private bool _gitMissing;

    public GitForm(string? path, GitSettings settings, string? settingsError)
        : base(AxGitApp.Name, settings, GitSettings.DefaultPath)
    {
        _settings = settings;
        _startPath = path;
        Text = "No repository - " + AxGitApp.Name;

        // ---- menus (AG-6.1); the keys are handled in ProcessWindowKey from the tables, the items only show them ----
        // Repository is the thing worked on, in the standard order of a first menu (bundle AX-6): Open and the recent
        // repositories, then the slot the editors have Save in (Refresh here), then the extras, then Exit.
        var repository = new ToolStripMenuItem("&Repository");
        repository.DropDownItems.Add(Item("&Open repository...", OpenRepositoryDialog, AxGitKeys.OpenRepository));
        CommandPalette.FilledOnOpening(_recentItem).DropDownOpening += (_, _) => FillRecent();
        FillRecent();
        repository.DropDownItems.Add(_recentItem);
        repository.DropDownItems.Add(new ToolStripSeparator());
        repository.DropDownItems.Add(Git(Item("Re&fresh", () => Do(() => RefreshAsync(announce: true)), AxGitKeys.Refresh)));
        repository.DropDownItems.Add(new ToolStripSeparator());
        // Show current repository in ▸ File Explorer, AxClaude, AxTerminal, AxFile: the way back to where the user came from (AG-1.4).
        var showIn = new ToolStripMenuItem("Show current repository &in");
        showIn.DropDownItems.Add(Item("File &Explorer", ShowInExplorer, AxGitKeys.ShowInExplorer));
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Claude", null, (_, _) => OpenInAxClaude()));
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Terminal", null, (_, _) => OpenInAxTerminal()));
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&File", null, (_, _) => OpenInAxFile()));
        repository.DropDownItems.Add(showIn);
        repository.DropDownItems.Add(new ToolStripSeparator());
        repository.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close()) { ShortcutKeyDisplayString = "Alt+F4" });

        var changes = new ToolStripMenuItem("&Changes");
        changes.DropDownItems.Add(Git(Item("&Stage or unstage", () => Do(StageToggleAsync), AxGitKeys.StageToggle)));
        changes.DropDownItems.Add(Git(Item("Stage &all", () => Do(() => StageAllAsync(stage: true)), AxGitKeys.StageAll)));
        changes.DropDownItems.Add(Git(Item("&Unstage all", () => Do(() => StageAllAsync(stage: false)), AxGitKeys.UnstageAll)));
        changes.DropDownItems.Add(new ToolStripSeparator());
        changes.DropDownItems.Add(Git(Item("Show &diff", () => Do(ShowDiffAsync), AxGitKeys.Act)));
        changes.DropDownItems.Add(Item("&Open file in editor", OpenInEditor, AxGitKeys.OpenInEditor));
        changes.DropDownItems.Add(Git(Item("Disca&rd changes...", Discard, AxGitKeys.Discard)));
        changes.DropDownItems.Add(new ToolStripSeparator());
        changes.DropDownItems.Add(Git(Item("&Commit...", Commit, AxGitKeys.Commit)));

        var branches = new ToolStripMenuItem("&Branches");
        branches.DropDownItems.Add(Git(Item("&Switch to branch", () => Do(SwitchBranchAsync), AxGitKeys.Act)));
        branches.DropDownItems.Add(Git(Item("&New branch...", NewBranch, AxGitKeys.NewBranch)));
        branches.DropDownItems.Add(new ToolStripSeparator());
        branches.DropDownItems.Add(Git(Item("&Fetch", () => Do(FetchAsync), AxGitKeys.Fetch)));
        branches.DropDownItems.Add(Git(Item("&Pull", () => Do(PullAsync), AxGitKeys.Pull)));
        branches.DropDownItems.Add(Git(Item("P&ush", () => Do(PushAsync), AxGitKeys.Push)));

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add(Item("&Changes", () => Do(() => ShowViewAsync(View.Changes)), AxGitKeys.ViewChanges));
        view.DropDownItems.Add(Item("&Log", () => Do(() => ShowViewAsync(View.Log)), AxGitKeys.ViewLog));
        view.DropDownItems.Add(Item("&Branches", () => Do(() => ShowViewAsync(View.Branches)), AxGitKeys.ViewBranches));

        // Options, as the other windows' (AX-6): every setting in the submenu for what it is about.
        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Git &resources",
            [("&git-scm.com", "https://git-scm.com"), ("&Pro Git, the book", "https://git-scm.com/book/en/v2")]);

        // The bundle's Axit menu (AX-9) sits before Help in every window and ends with the update items and About.
        _menu.Items.AddRange([repository, changes, branches, view, options, CreateAxitMenu(), help]);
        MainMenuStrip = _menu;

        // ---- the list and the details (AG-2.1) ----
        _list.IntegralHeight = false;
        _list.Dock = DockStyle.Top;
        _list.Height = 200;
        _list.TabIndex = 0;
        _list.AccessibleName = "No repository";

        _details.Multiline = true;
        _details.ReadOnly = true;
        _details.WordWrap = true;
        _details.ScrollBars = ScrollBars.Vertical;
        _details.HideSelection = false;
        _details.MaxLength = 0;
        _details.Dock = DockStyle.Fill;
        _details.TabIndex = 1;
        _details.AccessibleName = "Details";
        _details.KeyDown += (sender, e) => EditKeys.Handle(sender, e);

        // ---- the status bar (AG-2.3): the bundle's pair, no AccessibleName, no Spring ----
        (_stateLabel, _pathLabel) = StatusLayout.Pair(_status);

        SizeChanged += (_, _) => _list.Height = Math.Max(80, ClientSize.Height * 2 / 5);

        // Docked from the last control to the first: the menu takes the top, the status bar the bottom, the list the
        // strip under the menu, and the details (or the notice over both) what is left.
        Controls.AddRange([Overlay, _details, _list, _status, _menu]);

        ApplyTextFont();
        RestoreWindow();
        UpdateStatusBar();

        if (settingsError is not null)
        {
            ShowNotice(Notice.Plain("Settings", $"The settings file could not be read, so the defaults are used.\n{settingsError}") with { Unprompted = true });
        }
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice()
    {
        _list.Visible = false;
        _details.Visible = false;
    }

    protected override void ShowAfterNotice()
    {
        _list.Visible = true;
        _details.Visible = true;
    }

    protected override Control FocusTarget => _list;

    /// <summary>The details keep the focus across a notice when they had it.</summary>
    protected override Control? FocusToKeep() => _details.Focused ? _details : _list;

    protected override void ApplyFont(Font font)
    {
        _list.Font = font;
        _details.Font = font;
    }

    protected override IReadOnlyList<string> RestartArguments() => _root is null ? [Dispatch.AxGit] : [Dispatch.AxGit, _root];

    protected override string UpdateRestartText => $"closes {AxGitApp.Name}, installs it and starts it again on the same repository";

    protected override IEnumerable<string> DiagnosticsLines => [$"Repository: {_root ?? "(none)"}"];

    protected override void OnShown(EventArgs e)
    {
        if (!NoticeOpen)
        {
            _list.Select();
        }

        base.OnShown(e);
        Do(() => StartAsync(_startPath));
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_root is { } closing)
        {
            // AX-9.4: the repository joins the bundle's Recently closed list.
            RecentlyClosed.Remember(AxGitApp.Name, closing);
        }

        base.OnFormClosing(e);
    }

    // ---- keys (AG-6.2): every key comes from BundleKeys or AxGitKeys, and is handled here only ----

    protected override bool ProcessWindowKey(Keys keyData)
    {
        switch (keyData)
        {
            case BundleKeys.NextControl:
            case BundleKeys.PreviousControl:
            case BundleKeys.NextControlAlso:
                Control other = _list.Focused ? _details : _list;
                other.Focus();
                return true;
            case BundleKeys.Find:
                ShowFind(_details);
                return true;
            case BundleKeys.FindNext:
                FindNext(_details, 1);
                return true;
            case BundleKeys.FindPrevious:
                FindNext(_details, -1);
                return true;
            case BundleKeys.Save:
                // The bundle's save key, with nothing here to save (AG-6.2).
                Announce("Nothing to save here. Space stages, Ctrl+Enter commits", true);
                return true;
            case AxGitKeys.OpenRepository:
                OpenRepositoryDialog();
                return true;
            case AxGitKeys.Refresh:
                Do(() => RefreshAsync(announce: true));
                return true;
            case AxGitKeys.ShowInExplorer:
                ShowInExplorer();
                return true;
            case AxGitKeys.ViewChanges:
                Do(() => ShowViewAsync(View.Changes));
                return true;
            case AxGitKeys.ViewLog:
                Do(() => ShowViewAsync(View.Log));
                return true;
            case AxGitKeys.ViewBranches:
                Do(() => ShowViewAsync(View.Branches));
                return true;
            case AxGitKeys.StageAll:
                Do(() => StageAllAsync(stage: true));
                return true;
            case AxGitKeys.UnstageAll:
                Do(() => StageAllAsync(stage: false));
                return true;
            case AxGitKeys.OpenInEditor:
                OpenInEditor();
                return true;
            case AxGitKeys.Commit:
                Commit();
                return true;
            case AxGitKeys.NewBranch:
                NewBranch();
                return true;
            case AxGitKeys.Fetch:
                Do(FetchAsync);
                return true;
            case AxGitKeys.Pull:
                Do(PullAsync);
                return true;
            case AxGitKeys.Push:
                Do(PushAsync);
                return true;
        }

        if (_list.Focused)
        {
            // Space, Enter and Delete act in the list only; in the details they are the control's own (AG-6.2).
            switch (keyData)
            {
                case AxGitKeys.StageToggle:
                    Do(StageToggleAsync);
                    return true;
                case AxGitKeys.Act:
                    Do(ActOnItemAsync);
                    return true;
                case AxGitKeys.Discard:
                    Discard();
                    return true;
            }
        }

        return false;
    }

    // ---- the repository (AG-1.2) ----

    /// <summary>Runs a command's task on the UI thread and turns an escaped exception into a notice rather than a crash.</summary>
    private async void Do(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error("AxGit command failed", ex);
            if (!IsDisposed)
            {
                Fail(ex.Message);
            }
        }
    }

    private async Task StartAsync(string? path)
    {
        var target = path ?? _settings.RecentRepositories.FirstOrDefault(Directory.Exists);
        if (target is null)
        {
            SetNoRepository();
            return;
        }

        string? root;
        try
        {
            root = await GitRunner.RootAsync(target);
        }
        catch (GitNotFoundException)
        {
            GitMissing();
            return;
        }

        if (IsDisposed)
        {
            return;
        }

        if (root is null)
        {
            SetNoRepository();
            ShowNotice(Notice.Plain("Not a git repository", $"{target} is not inside a git repository. Repository, Open repository (Ctrl+O) takes another folder.") with { Unprompted = true });
            return;
        }

        await OpenRepositoryAsync(root);
    }

    private async Task OpenRepositoryAsync(string root)
    {
        _root = root;
        _selected.Clear();
        _commits = [];
        _branches = [];
        _logLoaded = false;
        _branchesLoaded = false;
        Text = $"{RecentMenu.FolderName(root)} - {AxGitApp.Name}";
        _settings.RememberRepository(root);
        SaveSettings();
        Log.Info($"Opened {root}");
        await RefreshAsync(announce: false);
    }

    private void SetNoRepository()
    {
        _root = null;
        _repo = RepositoryStatus.Empty;
        Text = "No repository - " + AxGitApp.Name;
        _list.Items.Clear();
        _list.AccessibleName = "No repository";
        UpdateStatusBar();
    }

    /// <summary>Repository → Open repository… (Ctrl+O): the Windows folder picker, one of the system's own windows; any folder inside a repository will do.</summary>
    private void OpenRepositoryDialog()
    {
        using var dialog = new FolderBrowserDialog { Description = "Choose a folder inside the repository", UseDescriptionForTitle = true };
        if (_root is not null)
        {
            dialog.InitialDirectory = _root;
        }

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            _list.Select();
            return;
        }

        _list.Select();
        Do(() => StartAsync(dialog.SelectedPath));
    }

    /// <summary>Repository → Open recent repository (AG-6.1): the last ten, those that still exist, "1. name (path)" (AX-9.5).</summary>
    private void FillRecent() =>
        RecentMenu.Fill(_recentItem, _settings.RecentRepositories, Directory.Exists, RecentMenu.FolderName, "&No recent repositories", target => Do(() => StartAsync(target)));

    /// <summary>Repository → Show current repository in → File Explorer (Ctrl+W): the folder opened in File Explorer, one of the system's own windows.</summary>
    private void ShowInExplorer()
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        OpenInExplorer(_root);
    }

    /// <summary>Repository → Show current repository in → AxClaude: a new AxClaude on the repository's folder (AG-1.4).</summary>
    private void OpenInAxClaude()
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        AxitMenu.Start(Dispatch.AxClaude, _root);
    }

    /// <summary>Repository → Show current repository in → AxTerminal: a new AxTerminal with a shell in the repository's folder (docs/axterminal/SPEC.md AT-1.5).</summary>
    private void OpenInAxTerminal()
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        AxitMenu.Start(Dispatch.AxTerminal, _root);
    }

    /// <summary>Repository → Show current repository in → AxFile: a new AxFile window listing the repository's folder (docs/axfile/SPEC.md AF-1.3).</summary>
    private void OpenInAxFile()
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        AxitMenu.Start(Dispatch.AxFile, _root);
    }

    /// <summary>Changes → Open file in editor (Ctrl+E): the selected file in AxJSON for .json, AxDown otherwise (AG-1.4).</summary>
    private void OpenInEditor()
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        if (CurrentFile() is not { } file)
        {
            Announce("No file selected", true);
            return;
        }

        var path = Path.Combine(_root, file.Path.Replace('/', Path.DirectorySeparatorChar));
        var verb = Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase) ? Dispatch.AxJSON : Dispatch.AxDown;
        AxitMenu.Start(verb, path);
    }

    // ---- running git (AG-1.5, AG-D6) ----

    /// <summary>
    /// One git command in the repository: the status bar says what runs, the git items are disabled meanwhile, and
    /// the result comes back for the caller to announce. Null when nothing ran: no repository, git missing, or a
    /// command still running ("Working…").
    /// </summary>
    private async Task<GitResult?> RunGitAsync(IReadOnlyList<string> arguments, TimeSpan timeout, string verb)
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return null;
        }

        if (_gitMissing)
        {
            GitMissing();
            return null;
        }

        if (_busy is not null)
        {
            Announce("Working…", true);
            return null;
        }

        _busy = verb;
        SetGitItems(false);
        UpdateStatusBar();
        var watch = Stopwatch.StartNew();
        try
        {
            var result = await GitRunner.RunAsync(_root, arguments, timeout);
            // The subcommand alone (AG-8.2): the arguments hold the commit message and the paths.
            Log.Info($"git {arguments[0]}: exit {result.ExitCode} in {watch.ElapsedMilliseconds} ms{(result.Ok ? string.Empty : ", " + result.FirstLine)}");
            return result;
        }
        catch (GitNotFoundException)
        {
            GitMissing();
            return null;
        }
        finally
        {
            _busy = null;
            if (!IsDisposed)
            {
                SetGitItems(true);
                UpdateStatusBar();
            }
        }
    }

    private void SetGitItems(bool enabled)
    {
        foreach (var item in _gitItems)
        {
            item.Enabled = enabled && !_gitMissing;
        }
    }

    /// <summary>git is not on the PATH (AG-1.5): a notice each time a command is asked for, with the chime the first time, and every command stays disabled.</summary>
    private void GitMissing()
    {
        var first = !_gitMissing;
        _gitMissing = true;
        SetGitItems(false);
        UpdateStatusBar();
        ShowNotice(new Notice("Git was not found",
            "AxGit runs the git program, and it is not on the PATH of this computer. Install Git for Windows from git-scm.com, then start AxGit again.",
        [
            new OverlayChoice("&Open git-scm.com", () => OpenUrl("https://git-scm.com"), StaysOpen: true),
            OverlayChoice.Close,
        ])
        {
            Unprompted = first,
        });
    }

    /// <summary>A failed command: one announcement when git said one line, a notice with everything it said otherwise (AG-7).</summary>
    private void Failed(string title, GitResult result)
    {
        var text = result.AllText;
        if (text.Contains('\n'))
        {
            ShowNotice(Notice.Plain(title, text) with { Unprompted = true });
        }
        else
        {
            Announce("Failed: " + result.FirstLine, true);
        }
    }

    // ---- the views (AG-2.2) ----

    private async Task RefreshAsync(bool announce)
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        var result = await RunGitAsync(GitStatus.Arguments, GitRunner.Short, "status");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Status failed", result);
            return;
        }

        // The selection by key, taken before the lists change under it (AG-2.2).
        _selected[_view] = CurrentKey() ?? _selected.GetValueOrDefault(_view);
        _repo = GitStatus.Parse(result.Output);
        if (_view == View.Log)
        {
            await LoadLogAsync();
        }
        else if (_view == View.Branches)
        {
            await LoadBranchesAsync();
        }

        FillList();
        UpdateStatusBar();
        if (announce)
        {
            Announce("Refreshed: " + _list.AccessibleName, true);
        }
    }

    private async Task LoadLogAsync()
    {
        var result = await RunGitAsync(GitLog.Arguments, GitRunner.Short, "log");
        if (result is { Ok: true })
        {
            _commits = GitLog.Parse(result.Output);
            _logLoaded = true;
        }
        else if (result is not null)
        {
            Failed("Log failed", result);
        }
    }

    private async Task LoadBranchesAsync()
    {
        var result = await RunGitAsync(GitBranches.Arguments, GitRunner.Short, "branches");
        if (result is { Ok: true })
        {
            _branches = GitBranches.Parse(result.Output);
            _branchesLoaded = true;
        }
        else if (result is not null)
        {
            Failed("Branches failed", result);
        }
    }

    /// <summary>View → Changes, Log, Branches (Ctrl+1, 2, 3): the list refilled, loaded on the first visit, and its name said with the item the focus is on (AxClaude D26).</summary>
    private async Task ShowViewAsync(View view)
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        if (_busy is not null)
        {
            // The view's first load would be refused and the list shown empty under the new name.
            Announce("Working…", true);
            return;
        }

        _selected[_view] = CurrentKey() ?? _selected.GetValueOrDefault(_view);
        _view = view;
        if (view == View.Log && !_logLoaded)
        {
            await LoadLogAsync();
        }
        else if (view == View.Branches && !_branchesLoaded)
        {
            await LoadBranchesAsync();
        }

        FillList();
        var name = _list.AccessibleName ?? string.Empty;
        Announce(_list.SelectedItem is string item ? $"{name}. {item}" : name, true);
    }

    /// <summary>The list's items for the view, named with the view and the count, the selection on the key kept for the view (<see cref="_selected"/>, taken by the caller before the lists changed).</summary>
    private void FillList()
    {
        var wanted = _selected.GetValueOrDefault(_view);
        _list.BeginUpdate();
        _list.Items.Clear();
        string name;
        switch (_view)
        {
            case View.Changes:
                foreach (var file in _repo.Files)
                {
                    _list.Items.Add(file.Label);
                }

                name = _repo.ListName;
                break;
            case View.Log:
                foreach (var commit in _commits)
                {
                    _list.Items.Add(commit.Label);
                }

                name = GitLog.ListName(_commits.Count);
                break;
            default:
                foreach (var branch in _branches)
                {
                    _list.Items.Add(branch.Label);
                }

                name = GitBranches.ListName(_branches.Count);
                break;
        }

        _list.EndUpdate();
        _list.AccessibleName = name;
        var keys = ItemKeys();
        var index = wanted is null ? -1 : keys.ToList().IndexOf(wanted);
        if (_list.Items.Count > 0)
        {
            _list.SelectedIndex = index >= 0 ? index : 0;
        }
    }

    /// <summary>The key of each item of the view: the file's path, the commit's hash, the branch's name.</summary>
    private IReadOnlyList<string> ItemKeys() => _view switch
    {
        View.Changes => _repo.Files.Select(file => file.Path).ToList(),
        View.Log => _commits.Select(commit => commit.Hash).ToList(),
        _ => _branches.Select(branch => branch.Name).ToList(),
    };

    /// <summary>The key of the selected item, read while the list still shows the view's lists as they are (before a refresh replaces them, before a view switch changes <see cref="_view"/>); null with nothing selected.</summary>
    private string? CurrentKey()
    {
        var keys = ItemKeys();
        var index = _list.SelectedIndex;
        return index >= 0 && index < keys.Count ? keys[index] : null;
    }

    private ChangedFile? CurrentFile() =>
        _view == View.Changes && _list.SelectedIndex is var i && i >= 0 && i < _repo.Files.Count ? _repo.Files[i] : null;

    private Commit? CurrentCommit() =>
        _view == View.Log && _list.SelectedIndex is var i && i >= 0 && i < _commits.Count ? _commits[i] : null;

    private Branch? CurrentBranch() =>
        _view == View.Branches && _list.SelectedIndex is var i && i >= 0 && i < _branches.Count ? _branches[i] : null;

    /// <summary>Enter in the list: the diff of a file, the details of a commit, the switch to a branch (AG-6.2).</summary>
    private Task ActOnItemAsync() => _view switch
    {
        View.Changes => ShowDiffAsync(),
        View.Log => ShowCommitAsync(),
        _ => SwitchBranchAsync(),
    };

    // ---- changes (AG-3) ----

    private async Task StageToggleAsync()
    {
        if (_view != View.Changes)
        {
            await ShowViewAsync(View.Changes);
            return;
        }

        if (CurrentFile() is not { } file)
        {
            Announce("No file selected", true);
            return;
        }

        if (!file.CanStage)
        {
            Announce("A conflict: resolve it in the editor or the terminal first", true);
            return;
        }

        var result = await RunGitAsync(file.StageArguments, GitRunner.Short, file.Unstaged ? "stage" : "unstage");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed(file.Unstaged ? "Stage failed" : "Unstage failed", result);
            return;
        }

        Announce(file.StageAnnouncement, true);
        await RefreshAsync(announce: false);
    }

    private async Task StageAllAsync(bool stage)
    {
        var result = await RunGitAsync(stage ? ["add", "-A"] : ["restore", "--staged", "."], GitRunner.Short, stage ? "stage all" : "unstage all");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed(stage ? "Stage failed" : "Unstage failed", result);
            return;
        }

        Announce(stage ? "Staged all" : "Unstaged all", true);
        await RefreshAsync(announce: false);
    }

    /// <summary>Enter on a file (AG-3.3): its diff in the details, with the focus on the first line; an untracked file shows itself.</summary>
    private async Task ShowDiffAsync()
    {
        if (_view != View.Changes)
        {
            await ShowViewAsync(View.Changes);
            return;
        }

        if (_root is null || CurrentFile() is not { } file)
        {
            Announce(_root is null ? "No repository" : "No file selected", true);
            return;
        }

        if (file.Kind == ChangeKind.Untracked)
        {
            var path = Path.Combine(_root, file.Path.Replace('/', Path.DirectorySeparatorChar));
            string text;
            try
            {
                text = await File.ReadAllTextAsync(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Fail($"{file.Path} could not be read: {ex.Message}");
                return;
            }

            if (text.Length > UntrackedLimit)
            {
                text = text[..UntrackedLimit] + "\n(cut after 200 KB)";
            }

            var lines = text.Length == 0 ? 0 : text.Split('\n').Length;
            ShowDetails($"Diff of {file.Path}", $"Untracked file, {lines} lines\n{text}", focus: true);
            return;
        }

        var result = await RunGitAsync(file.DiffArguments, GitRunner.Short, "diff");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Diff failed", result);
            return;
        }

        if (result.Output.Trim().Length == 0)
        {
            Announce("No changes", true);
            return;
        }

        ShowDetails($"Diff of {file.Path}", result.Output, focus: true);
    }

    /// <summary>Changes → Discard changes… (Delete, AG-3.4): a question first; an untracked file is not deleted from here.</summary>
    private void Discard()
    {
        if (CurrentFile() is not { } file)
        {
            Announce(_view == View.Changes ? "No file selected" : "Changes view first: Ctrl+1", true);
            return;
        }

        if (file.Kind == ChangeKind.Untracked)
        {
            Announce("Untracked file: delete it in File Explorer", true);
            return;
        }

        ShowNotice(new Notice($"Discard the changes to {file.Path}?", "They cannot be brought back.",
        [
            new OverlayChoice("&Discard", () => Do(async () =>
            {
                var result = await RunGitAsync(file.DiscardArguments, GitRunner.Short, "discard");
                if (result is null)
                {
                    return;
                }

                if (!result.Ok)
                {
                    Failed("Discard failed", result);
                    return;
                }

                Announce($"Discarded {file.Path}", true);
                await RefreshAsync(announce: false);
            }), IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ]));
    }

    /// <summary>Changes → Commit… (Ctrl+Enter, AG-3.5): the message in a notice, then git commit.</summary>
    private void Commit()
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        var staged = _repo.StagedCount;
        if (staged == 0)
        {
            Announce("Nothing staged. Space stages a file", true);
            return;
        }

        ShowNotice(new Notice($"Commit {staged} staged {(staged == 1 ? "file" : "files")}", string.Empty,
        [
            new OverlayChoice("&Commit", () =>
            {
                var message = Overlay.InputText.Trim();
                Do(async () =>
                {
                    var result = await RunGitAsync(["commit", "-m", message], GitRunner.Short, "commit");
                    if (result is null)
                    {
                        return;
                    }

                    if (!result.Ok)
                    {
                        Failed("Commit failed", result);
                        return;
                    }

                    var hash = CommitHashRegex().Match(result.Output);
                    Announce(hash.Success ? $"Committed {hash.Groups[1].Value}" : "Committed", true);
                    _logLoaded = false;
                    _branchesLoaded = false;
                    await RefreshAsync(announce: false);
                });
            }, IsDefault: true),
            new OverlayChoice("C&ancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Message:", "Message", string.Empty, "Type the commit message"),
        });
    }

    // "[main 3a1f2c0] Subject" on the first line of git commit's output.
    [GeneratedRegex(@"^\[[^\]]+ ([0-9a-f]{7,})\]", RegexOptions.Multiline)]
    private static partial Regex CommitHashRegex();

    // ---- history (AG-4) ----

    private async Task ShowCommitAsync()
    {
        if (CurrentCommit() is not { } commit)
        {
            Announce("No commit selected", true);
            return;
        }

        var result = await RunGitAsync(commit.ShowArguments, GitRunner.Short, "show");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Show failed", result);
            return;
        }

        ShowDetails($"Commit {commit.Hash}", result.Output, focus: true);
    }

    // ---- branches (AG-5) ----

    private async Task SwitchBranchAsync()
    {
        if (_view != View.Branches)
        {
            await ShowViewAsync(View.Branches);
            return;
        }

        if (CurrentBranch() is not { } branch)
        {
            Announce("No branch selected", true);
            return;
        }

        if (branch.Current)
        {
            Announce($"Already on {branch.Name}", true);
            return;
        }

        var result = await RunGitAsync(branch.SwitchArguments, GitRunner.Short, "switch");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Switch failed", result);
            return;
        }

        Announce($"Switched to {branch.Name}", true);
        _logLoaded = false;
        _branchesLoaded = false;
        await RefreshAsync(announce: false);
    }

    /// <summary>Branches → New branch… (Ctrl+N, AG-5.3): the name in a notice, then git switch -c.</summary>
    private void NewBranch()
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        ShowNotice(new Notice("New branch", "A new branch from the current commit, checked out at once.",
        [
            new OverlayChoice("C&reate", () =>
            {
                var name = Overlay.InputText.Trim();
                Do(async () =>
                {
                    var result = await RunGitAsync(GitBranches.CreateArguments(name), GitRunner.Short, "new branch");
                    if (result is null)
                    {
                        return;
                    }

                    if (!result.Ok)
                    {
                        Failed("New branch failed", result);
                        return;
                    }

                    Announce($"Switched to {name}", true);
                    _logLoaded = false;
                    _branchesLoaded = false;
                    await RefreshAsync(announce: false);
                });
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Name:", "Name", string.Empty, "Type the branch name"),
        });
    }

    private async Task FetchAsync()
    {
        var result = await RunGitAsync(["fetch", "--prune"], GitRunner.Long, "fetch");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Fetch failed", result);
            return;
        }

        Announce("Fetched", true);
        _branchesLoaded = false;
        await RefreshAsync(announce: false);
    }

    private async Task PullAsync()
    {
        var result = await RunGitAsync(["pull", "--ff-only"], GitRunner.Long, "pull");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Pull failed", result);
            return;
        }

        Announce(result.Output.Contains("Already up to date", StringComparison.OrdinalIgnoreCase) ? "Already up to date" : "Pulled", true);
        _logLoaded = false;
        _branchesLoaded = false;
        await RefreshAsync(announce: false);
    }

    private async Task PushAsync()
    {
        IReadOnlyList<string> arguments = _repo.Upstream is null && _repo.Branch is { } branch
            ? ["push", "--set-upstream", "origin", branch]
            : ["push"];
        var result = await RunGitAsync(arguments, GitRunner.Long, "push");
        if (result is null)
        {
            return;
        }

        if (!result.Ok)
        {
            Failed("Push failed", result);
            return;
        }

        Announce("Pushed", true);
        _branchesLoaded = false;
        await RefreshAsync(announce: false);
    }

    // ---- the details and the status bar (AG-2.1, AG-2.3, AG-2.4) ----

    /// <summary>Text into the details, named for what it is; with <paramref name="focus"/> the focus goes to its first line and the name and length are said after it (AG-D5).</summary>
    private void ShowDetails(string name, string text, bool focus)
    {
        var normalized = text.Replace("\r\n", "\n").TrimEnd('\n').Replace("\n", "\r\n");
        var lines = normalized.Length == 0 ? 0 : normalized.Split("\r\n").Length;
        _details.AccessibleName = name;
        _details.Text = normalized;
        _details.Select(0, 0);
        _details.ScrollToCaret();
        if (focus)
        {
            _details.Focus();
        }

        Announce($"{name}, {lines} {(lines == 1 ? "line" : "lines")}", !focus);
    }

    private void UpdateStatusBar()
    {
        _stateLabel.Text = _busy is not null ? $"Working: {_busy}…"
            : _gitMissing ? "git was not found"
            : _root is null ? "No repository"
            : _repo.StatusText;
        _pathLabel.Text = _root ?? string.Empty;
        StatusLayout.Fit(_status, _stateLabel, _pathLabel);
    }

    /// <summary>An item that runs git: disabled while a command runs and when git is missing.</summary>
    private ToolStripMenuItem Git(ToolStripMenuItem item)
    {
        _gitItems.Add(item);
        return item;
    }
}

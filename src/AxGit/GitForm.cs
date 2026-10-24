using System.Diagnostics;
using System.Text.RegularExpressions;
using AxGit.Core;
using Axit.Core;
using Axit.Forms;

namespace AxGit;

/// <summary>
/// axGIT's window (SPEC.md AG-2): a menu bar, one list with three views (Changes, Log, Branches), one read-only text
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

    /// <summary>The repository's own programs set to nothing while it is not trusted (AG-1.6); empty for a trusted one.</summary>
    private IReadOnlyList<string> _overrides = [];

    private readonly GitSettings _settings;
    private readonly string? _startPath;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _recentItem = new("Open &recent repository");
    private readonly AxitList _list = new();
    private readonly TextBox _details = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _stateLabel;
    private readonly ToolStripStatusLabel _pathLabel;

    /// <summary>The menu items that run git, disabled while a command runs and when git is missing (AG-5.4).</summary>
    private readonly List<ToolStripMenuItem> _gitItems = [];

    /// <summary>The three views, indexed by <see cref="View"/>; <see cref="Current"/> is the one the list shows.</summary>
    /// <summary>The three views of the list (AG-2.2), the whole state of the list: keys the file's path, the commit's hash, the branch's name; made stale by a commit, a switch, a fetch.</summary>
    private readonly ListViewState[] _views = [new(), new(), new()];

    private string? _root;
    private View _view = View.Changes;
    private RepositoryStatus _repo = RepositoryStatus.Empty;
    private IReadOnlyList<Commit> _commits = [];
    private IReadOnlyList<Branch> _branches = [];

    /// <summary>The verb of the git command running now, for the status bar; null between commands.</summary>
    private string? _busy;

    private bool _gitMissing;

    public GitForm(string? path, GitSettings settings, string? settingsError)
        : base(AxGitApp.Name, settings, GitSettings.DefaultPath, settingsError)
    {
        _settings = settings;
        _startPath = path;
        Text = "No repository - " + AxGitApp.Name;

        // ---- menus and keys (AG-6.1, AG-6.2): one command list (docs/axit/app-kit.md AK-2), each key from the tables ----
        // Repository is the thing worked on, in the standard order of a first menu (bundle AX-6): Open and the recent
        // repositories, then the slot the editors have Save in (Refresh here), then the extras, then Exit. The keys act
        // while a menu item is disabled, as before: the command says what stops it.
        var repository = new ToolStripMenuItem("&Repository");
        Add(repository, new Command("&Open repository...", AxGitKeys.OpenRepository, OpenRepositoryDialog) { Mark = CommandMark.Open });
        CommandPalette.FilledOnOpening(_recentItem).DropDownOpening += (_, _) => FillRecent();
        FillRecent();
        repository.DropDownItems.Add(_recentItem);
        repository.DropDownItems.Add(new ToolStripSeparator());
        Git(Add(repository, new Command("Re&fresh", AxGitKeys.Refresh, () => Do(() => RefreshAsync(announce: true))) { Mark = CommandMark.Read }));
        repository.DropDownItems.Add(new ToolStripSeparator());
        // Show current repository in ▸ File Explorer, axClaude, axTerminal, axFile: the way back to where the user came
        // from (AG-1.4). The base makes the submenu around its first item, so File Explorer is a key of the list and that item.
        var explorer = new Command("File &Explorer", AxGitKeys.ShowInExplorer, ShowInExplorer) { Mark = CommandMark.Open };
        repository.DropDownItems.Add(CreateShowInMenu("Show current repository &in", Add(explorer), RepositoryOrSay));
        repository.DropDownItems.Add(new ToolStripSeparator());
        repository.DropDownItems.Add(CreateExitItem());

        // Space, Enter and Delete act in the list only; in the details they are the control's own (AG-6.2). Enter acts
        // on the item of the view shown: Show diff, the commit (a key without an item), Switch to branch.
        var changes = new ToolStripMenuItem("&Changes");
        Git(Add(changes, new Command("&Stage or unstage", AxGitKeys.StageToggle, () => Do(StageToggleAsync)) { When = () => _list.Focused }));
        Git(Add(changes, new Command("Stage &all", AxGitKeys.StageAll, () => Do(() => StageAllAsync(stage: true)))));
        Git(Add(changes, new Command("&Unstage all", AxGitKeys.UnstageAll, () => Do(() => StageAllAsync(stage: false)))));
        changes.DropDownItems.Add(new ToolStripSeparator());
        Git(Add(changes, new Command("Show &diff", AxGitKeys.Act, () => Do(ShowDiffAsync)) { When = () => _list.Focused && _view == View.Changes, Mark = CommandMark.Read }));
        Add(changes, new Command("&Open file in editor", AxGitKeys.OpenInEditor, OpenInEditor) { Mark = CommandMark.Open });
        Git(Add(changes, new Command("Disca&rd changes...", AxGitKeys.Discard, Discard) { When = () => _list.Focused, Mark = CommandMark.Act }));
        changes.DropDownItems.Add(new ToolStripSeparator());
        Git(Add(changes, new Command("&Commit...", AxGitKeys.Commit, Commit) { Mark = CommandMark.Act }));

        var branches = new ToolStripMenuItem("&Branches");
        Git(Add(branches, new Command("&Switch to branch", AxGitKeys.Act, () => Do(SwitchBranchAsync)) { When = () => _list.Focused && _view == View.Branches, Mark = CommandMark.Act }));
        Git(Add(branches, new Command("&New branch...", AxGitKeys.NewBranch, NewBranch) { Mark = CommandMark.Act }));
        branches.DropDownItems.Add(new ToolStripSeparator());
        Git(Add(branches, new Command("&Fetch", AxGitKeys.Fetch, () => Do(FetchAsync)) { Mark = CommandMark.Act }));
        Git(Add(branches, new Command("&Pull", AxGitKeys.Pull, () => Do(PullAsync)) { Mark = CommandMark.Act }));
        Git(Add(branches, new Command("P&ush", AxGitKeys.Push, () => Do(PushAsync)) { Mark = CommandMark.Act }));

        var view = new ToolStripMenuItem("&View");
        Add(view, new Command("&Changes", AxGitKeys.ViewChanges, () => Do(() => ShowViewAsync(View.Changes))) { Mark = CommandMark.Open });
        Add(view, new Command("&Log", AxGitKeys.ViewLog, () => Do(() => ShowViewAsync(View.Log))) { Mark = CommandMark.Open });
        Add(view, new Command("&Branches", AxGitKeys.ViewBranches, () => Do(() => ShowViewAsync(View.Branches))) { Mark = CommandMark.Open });

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

        // The keys without a menu item: between the list and the details, find in the details, Enter on a commit.
        foreach (var key in new[] { BundleKeys.NextControl, BundleKeys.PreviousControl, BundleKeys.NextControlAlso })
        {
            AddKey(new Command("Switch between the list and the details", key, () => (_list.Focused ? (Control)_details : _list).Focus()) { Mark = CommandMark.Open });
        }

        AddKey(new Command("Find in the details", BundleKeys.Find, () => ShowFind(_details)) { Mark = CommandMark.Read });
        AddKey(new Command("Find next", BundleKeys.FindNext, () => FindNext(_details, 1)) { Mark = CommandMark.Read });
        AddKey(new Command("Find previous", BundleKeys.FindPrevious, () => FindNext(_details, -1)) { Mark = CommandMark.Read });
        AddKey(new Command("Show the commit", AxGitKeys.Act, () => Do(ShowCommitAsync)) { When = () => _list.Focused && _view == View.Log, Mark = CommandMark.Read });

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

    /// <summary>Ctrl+S, the bundle's save key: nothing to save in a git window, and the keys that do something said (AG-6.2).</summary>
    protected override void SaveCommand() => Announce("Nothing to save here. Space stages, Ctrl+Enter commits", true);

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

    /// <summary>The repository, for Recently closed (AX-9.4).</summary>
    protected override string? PathInHand => _root;

    // ---- the repository (AG-1.2) ----

    private async Task StartAsync(string? path)
    {
        var target = path ?? _settings.RecentRepositories.FirstOrDefault(Directory.Exists);
        if (target is null)
        {
            SetNoRepository();
            return;
        }

        if (!Directory.Exists(target))
        {
            // A recent repository on a share that is gone, listed without the check (AX-9.5): said, the open one kept.
            Fail($"{target} no longer exists.");
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
            // AG-1.2: the notice names the folder; a repository already open stays (until 2026-10-23 it was dropped first).
            if (_root is null)
            {
                SetNoRepository();
            }

            ShowNotice(Notice.Plain("Not a git repository", $"{target} is not inside a git repository. Repository, Open repository (Ctrl+O) takes another folder.") with { Unprompted = true });
            return;
        }

        await OpenRepositoryAsync(root);
    }

    private async Task OpenRepositoryAsync(string root)
    {
        _root = root;
        for (var i = 0; i < _views.Length; i++)
        {
            _views[i] = new ListViewState();
        }

        _repo = RepositoryStatus.Empty;
        _commits = [];
        _branches = [];
        Text = $"{PathName.Of(root)} - {AxGitApp.Name}";
        _settings.RememberRepository(root);
        SaveSettings();
        Log.Info($"Opened {root}");
        await CheckTrustAsync(root);
        await RefreshAsync(announce: false);
    }

    /// <summary>
    /// AG-1.6 (security audit S-2): what in the repository's own configuration and hooks runs a program, read before
    /// any other command; unless the user trusts the repository, every command sets each of them to nothing, and a
    /// notice says what is set aside, with the way to trust it.
    /// </summary>
    private async Task CheckTrustAsync(string root)
    {
        _overrides = [];
        try
        {
            var config = await GitRunner.RunAsync(root, GitTrust.ConfigArguments, GitRunner.Short);
            var hooks = await GitRunner.RunAsync(root, GitTrust.HooksArguments, GitRunner.Short);
            var hooksFolder = hooks.Ok && hooks.Output.Trim() is { Length: > 0 } folder ? Path.GetFullPath(folder, root) : null;
            var risks = GitTrust.Risks(config.Ok ? config.Output : string.Empty, hooksFolder);
            if (risks.Count == 0 || _settings.Trusts(root) || IsDisposed)
            {
                return;
            }

            _overrides = GitTrust.Overrides(risks);
            Log.Info($"Untrusted repository: {risks.Count} programs of its own set aside");
            ShowNotice(new Notice("This repository runs programs of its own",
                $"Its settings or hooks name programs git would run: {string.Join(", ", risks)}. axGIT runs git without them. Trust the repository only if you know where it comes from: then git runs them as it would in a console.",
            [
                new OverlayChoice("Keep them &off", IsDefault: true, IsCancel: true),
                new OverlayChoice("&Trust this repository", () => TrustRepository(root)),
            ]) { Unprompted = true });
        }
        catch (GitNotFoundException)
        {
            // git missing is said by the first command that needs it.
        }
    }

    private void TrustRepository(string root)
    {
        _settings.TrustedRepositories.Add(root);
        SaveSettings();
        _overrides = [];
        Announce("Trusted", true);
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

    /// <summary>Repository → Open repository… (Ctrl+O): the bundle's folder picker, one of the system's own windows; any folder inside a repository will do.</summary>
    private void OpenRepositoryDialog()
    {
        var chosen = ChooseFolder("Choose a folder inside the repository", _root);
        _list.Select();
        if (chosen is not null)
        {
            Do(() => StartAsync(chosen));
        }
    }

    /// <summary>Repository → Open recent repository (AG-6.1): the last ten, those that still exist, "1. name (path)" (AX-9.5).</summary>
    private void FillRecent() =>
        RecentMenu.FillFolders(_recentItem, _settings.RecentRepositories, "&No recent repositories", target => Do(() => StartAsync(target)));

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

    /// <summary>Repository → Show current repository in → axClaude, axTerminal, axFile (AG-1.4): a new window of that app on the repository's folder.</summary>
    private string? RepositoryOrSay()
    {
        if (_root is null)
        {
            Announce("No repository", true);
        }

        return _root;
    }

    /// <summary>Changes → Open file in editor (Ctrl+E): the selected file in axJSON for .json, axDown otherwise (AG-1.4).</summary>
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
        AxitMenu.Start(Dispatch.EditorOf(path), path);
    }
}

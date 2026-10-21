using AxFile.Core;
using Axit.Core;
using Axit.Forms;

namespace AxFile;

/// <summary>
/// AxFile's window (SPEC.md AF-2): a menu bar, the video area shown only while a video plays, one list with a folder's
/// entries, and a status bar with the player's state and the folder's path. Enter is the verb (G2): a folder opens in
/// the list, a file in its app or in the player. The folder reads and the operations run on background tasks; the
/// player's events come on this thread. The notices, the fonts, the window's place, the update flow and the
/// announcements are the bundle's window base (<see cref="AxitForm"/>). The window is in four files: this one (the
/// controls, the menus, the hooks, the keys), Browse (the folder, the list and the selection), Operations (the
/// operations and the ways out) and Player (the player).
/// </summary>
public sealed partial class FileForm : AxitForm
{
    private readonly FileSettings _settings;
    private readonly string? _startPath;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _recentItem = new("Open &recent folder");
    private readonly ToolStripMenuItem _favoritesItem = new("Fa&vourites");
    private readonly List<(SortBy By, ToolStripMenuItem Item)> _sortItems = [];
    private readonly ItemMenu _itemMenu;
    private readonly ListBox _list = new();
    private readonly TextBox _renameBox = new();
    private readonly Panel _video = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _stateLabel;
    private readonly ToolStripStatusLabel _pathLabel;

    /// <param name="settingsPath">Where the settings are saved; the app's own file unless a probe says otherwise, so that a probe never writes the user's.</param>
    public FileForm(string? path, FileSettings settings, string? settingsError, string? settingsPath = null)
        : base(AxFileApp.Name, settings, settingsPath ?? FileSettings.DefaultPath)
    {
        _settings = settings;
        _startPath = path;
        Text = AxFileApp.Name;

        // ---- menus (AF-6.1); the keys are handled in ProcessWindowKey from the tables, the items only show them ----
        // Folder is the thing worked on, in the standard order of a first menu (bundle AX-6): Open and the recent
        // folders, the ways around, Show current folder in, Exit.
        var folder = new ToolStripMenuItem("&Folder");
        folder.DropDownItems.Add(Item("&New window", NewWindow, AxFileKeys.NewWindow));
        folder.DropDownItems.Add(Item("&Open folder...", OpenFolderDialog, AxFileKeys.OpenFolder));
        _recentItem.DropDownOpening += (_, _) => FillRecent();
        FillRecent();
        folder.DropDownItems.Add(_recentItem);
        // AF-3.9: the pinned folders, refilled when the submenu opens, with the key's item under them.
        _favoritesItem.DropDownOpening += (_, _) => FillFavorites();
        FillFavorites();
        folder.DropDownItems.Add(_favoritesItem);
        folder.DropDownItems.Add(Item("&Go to folder...", GoToFolder, AxFileKeys.GoToFolder));
        folder.DropDownItems.Add(new ToolStripSeparator());
        folder.DropDownItems.Add(Item("&Up one level", GoUp, AxFileKeys.Up));
        folder.DropDownItems.Add(Item("Re&fresh", () => Do(() => RefreshAsync(announce: true)), AxFileKeys.Refresh));
        folder.DropDownItems.Add(new ToolStripSeparator());
        // AF-3.8: the order, one checked item, and the filter.
        var sort = new ToolStripMenuItem("&Sort by");
        foreach (var by in Enum.GetValues<SortBy>())
        {
            var sortItem = new ToolStripMenuItem("&" + char.ToUpperInvariant(by.ToString()[0]) + by.ToString()[1..], null, (_, _) => SetSort(by)) { Checked = _settings.SortBy == by };
            _sortItems.Add((by, sortItem));
            sort.DropDownItems.Add(sortItem);
        }

        folder.DropDownItems.Add(sort);
        folder.DropDownItems.Add(Item("Fil&ter...", ShowFilterNotice, AxFileKeys.Filter));
        folder.DropDownItems.Add(new ToolStripSeparator());
        // AF-1.3: the way to the other windows, on the folder shown.
        var showIn = new ToolStripMenuItem("Show current folder &in");
        showIn.DropDownItems.Add(Item("File &Explorer", ShowFolderInExplorer, AxFileKeys.ShowInExplorer));
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Claude", null, (_, _) => OpenIn(Dispatch.AxClaude)));
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Git", null, (_, _) => OpenIn(Dispatch.AxGit)));
        showIn.DropDownItems.Add(new ToolStripMenuItem("Ax&Terminal", null, (_, _) => OpenIn(Dispatch.AxTerminal)));
        folder.DropDownItems.Add(showIn);
        folder.DropDownItems.Add(new ToolStripSeparator());
        folder.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close()) { ShortcutKeyDisplayString = "Alt+F4" });

        // Item is the entry's menu (AF-4.6): the bar's drop-down and the list's context menu, filled the same way, so
        // that the two are one menu. The bar's drop-down is the one WinForms makes (AX-6.6): a hand-made one loses
        // its owner item when it closes, and the bar then opens its neighbours' menus on Left and Right.
        var item = new ToolStripMenuItem("&Item");
        FillItemMenu(item.DropDownItems);
        _itemMenu = new ItemMenu(SayInformation) { AccessibleName = "Item" };
        FillItemMenu(_itemMenu.Items);

        var player = new ToolStripMenuItem("&Player");
        player.DropDownItems.Add(Item("&Play", PlaySelected, AxFileKeys.Open));
        player.DropDownItems.Add(Item("P&ause or resume", PauseResume, AxFileKeys.PauseResume));
        player.DropDownItems.Add(Item("&Next file", () => MoveInQueue(1), AxFileKeys.NextFile));
        player.DropDownItems.Add(Item("Pre&vious file", () => MoveInQueue(-1), AxFileKeys.PreviousFile));
        player.DropDownItems.Add(Item($"&Forward {_settings.SeekSeconds} seconds", () => Seek(_settings.SeekSeconds), AxFileKeys.Forward));
        player.DropDownItems.Add(Item($"&Back {_settings.SeekSeconds} seconds", () => Seek(-_settings.SeekSeconds), AxFileKeys.Back));
        player.DropDownItems.Add(Item("&Go to time...", GoToTime, AxFileKeys.GoToTime));
        player.DropDownItems.Add(new ToolStripSeparator());
        player.DropDownItems.Add(Item("Fa&ster", () => ChangeSpeed(MediaFiles.Faster(_settings.Speed)), AxFileKeys.Faster));
        player.DropDownItems.Add(Item("S&lower", () => ChangeSpeed(MediaFiles.Slower(_settings.Speed)), AxFileKeys.Slower));
        player.DropDownItems.Add(new ToolStripMenuItem("N&ormal speed", null, (_, _) => ChangeSpeed(1)));
        player.DropDownItems.Add(new ToolStripSeparator());
        player.DropDownItems.Add(Toggle("Say &each file as it starts", _settings.SayEachFile, v => _settings.SayEachFile = v));
        player.DropDownItems.Add(Item("&What is playing", SayWhatIsPlaying, AxFileKeys.WhatIsPlaying));

        // Options, as the other windows' (AX-6): every setting in the submenu for what it is about.
        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(CreateTextMenu());
        // AF-2.2: what the list leaves out, two switches; the folder is read again on a flip.
        options.DropDownItems.Add(CreateGeneralMenu(
            Toggle("Show &hidden files and folders", _settings.ShowHidden, v =>
            {
                _settings.ShowHidden = v;
                Do(() => RefreshAsync(announce: false));
            }),
            Toggle("Show files and folders starting with a &period", _settings.ShowDotFiles, v =>
            {
                _settings.ShowDotFiles = v;
                Do(() => RefreshAsync(announce: false));
            })));
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "File &resources",
            [("&Keyboard shortcuts in Windows", "https://support.microsoft.com/windows/keyboard-shortcuts-in-windows-dcc61a57-8ff0-cffe-9796-cb9706c75eec")]);

        // The bundle's Axit menu (AX-9) sits before Help in every window and ends with the update items and About.
        _menu.Items.AddRange([folder, item, player, options, CreateAxitMenu(), help]);
        MainMenuStrip = _menu;

        // ---- the list (AF-2.1, AF-2.2): the one control; its own keys stay native (AF-3.6) ----
        _list.IntegralHeight = false;
        _list.Dock = DockStyle.Fill;
        _list.TabIndex = 0;
        _list.AccessibleName = "No folder";
        _list.DoubleClick += (_, _) => OpenSelected();
        // AF-4.6: Shift+F10, the Applications key and a right click open the Item menu as the list's context menu,
        // the list's own way (the keys are in the table for the collision test and F1, not in the key switch); a
        // right click first selects the entry under it, as File Explorer does.
        _list.ContextMenuStrip = _itemMenu;
        _list.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right && _list.IndexFromPoint(e.Location) is var index && index >= 0)
            {
                _list.SelectedIndex = index;
            }
        };

        // ---- the rename box (AF-4.2): a child of the list, placed over the entry while a rename runs, hidden otherwise ----
        _renameBox.Visible = false;
        _renameBox.AccessibleName = "New name";
        _renameBox.BorderStyle = BorderStyle.FixedSingle;
        _renameBox.KeyDown += OnRenameKey;
        _renameBox.Leave += (_, _) => CancelRename();
        _list.Controls.Add(_renameBox);

        // ---- the video area (AF-5.6): a plain surface at the top, shown while a video plays, never focused ----
        _video.Dock = DockStyle.Top;
        _video.Height = 0;
        _video.Visible = false;
        _video.BackColor = Color.Black;
        _video.TabStop = false;
        _video.AccessibleName = "Video";
        _video.AccessibleRole = AccessibleRole.Graphic;
        _video.Resize += (_, _) => _player?.UpdateVideo();
        _video.Paint += (_, _) => _player?.UpdateVideo();

        // ---- the status bar (AF-2.3): the bundle's pair, no AccessibleName, no Spring ----
        (_stateLabel, _pathLabel) = StatusLayout.Pair(_status);
        _clock.Tick += (_, _) => UpdateStatusBar();

        SizeChanged += (_, _) => FitVideo();
        // AF-3.4: coming back to the window reads the folder again when another program changed it.
        Activated += (_, _) => Do(RefreshIfChangedAsync);

        // Docked from the last control to the first: the menu takes the top, the status bar the bottom, the video
        // area the strip under the menu while it shows, and the list (or the notice over it) what is left.
        Controls.AddRange([Overlay, _list, _video, _status, _menu]);

        ApplyTextFont();
        RestoreWindow();
        UpdateStatusBar();

        if (settingsError is not null)
        {
            ShowNotice(Notice.Plain("Settings", $"The settings file could not be read, so the defaults are used.\n{settingsError}") with { Unprompted = true });
        }
    }

    /// <summary>
    /// The entry's menu (AF-4.6, AF-6.1), filled for the menu bar and again for the list's context menu: one flat
    /// list in File Explorer's order and words, with two submenus for the ways to open (Open with: Windows and the
    /// user's programs, AF-4.7, AF-4.11; Open in Axit: the bundle's apps, AF-4.9).
    /// </summary>
    private void FillItemMenu(ToolStripItemCollection items)
    {
        items.Add(Item("&Open", OpenSelected, AxFileKeys.Open));
        var openWith = new ToolStripMenuItem("Open &with");
        openWith.DropDownOpening += (_, _) => FillOpenWith(openWith);
        FillOpenWith(openWith);
        items.Add(openWith);
        var axit = new ToolStripMenuItem("Open in &Axit");
        axit.DropDownItems.Add(Item("&Editor", OpenInEditor, AxFileKeys.OpenInEditor));
        axit.DropDownItems.Add(new ToolStripSeparator());
        foreach (var app in Dispatch.Apps)
        {
            axit.DropDownItems.Add(new ToolStripMenuItem("Ax&" + app.Name[2..], null, (_, _) => OpenInAxit(app.Verb)));
        }

        items.Add(axit);
        items.Add(new ToolStripSeparator());
        items.Add(Item("Cu&t", () => CopyOrCut(cut: true), AxFileKeys.Cut));
        items.Add(Item("&Copy", () => CopyOrCut(cut: false), AxFileKeys.Copy));
        items.Add(Item("&Paste", Paste, AxFileKeys.Paste));
        items.Add(new ToolStripSeparator());
        // Posted, so that the menu has closed and the list has the focus back before the box takes it.
        items.Add(Item("Rena&me", () => BeginInvoke(() => Rename()), AxFileKeys.Rename));
        items.Add(Item("&Delete", Delete, AxFileKeys.Delete));
        items.Add(new ToolStripSeparator());
        items.Add(Item("&New folder...", NewFolder, AxFileKeys.NewFolder));
        items.Add(Item("New text &file", NewFile, AxFileKeys.NewFile));
        items.Add(new ToolStripMenuItem("Mo&ve to new folder...", null, (_, _) => MoveIntoNewFolder()));
        items.Add(new ToolStripMenuItem("Compress to &zip", null, (_, _) => Compress()));
        items.Add(new ToolStripMenuItem("E&xtract here", null, (_, _) => Extract()));
        items.Add(new ToolStripSeparator());
        items.Add(Item("&Select all", SelectAll, AxFileKeys.SelectAll));
        items.Add(Item("Select non&e", SelectNone, AxFileKeys.SelectNone));
        items.Add(new ToolStripSeparator());
        items.Add(Item("Copy pat&h", CopyPath, AxFileKeys.CopyPath));
        items.Add(Item("Show in File Exp&lorer", ShowItemInExplorer, AxFileKeys.ShowItemInExplorer));
        items.Add(Item("&Information", SayInformation, AxFileKeys.Information));
        items.Add(Item("Prope&rties", ShowProperties, AxFileKeys.Properties));
        items.Add(new ToolStripSeparator());
        // AF-4.7: the shell's own menu, behind one item of ours (AF-D8).
        items.Add(new ToolStripMenuItem("Windows men&u...", null, (_, _) => ShowWindowsMenu()));
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice()
    {
        CancelRename();
        _list.Visible = false;
        _video.Visible = false;
    }

    protected override void ShowAfterNotice()
    {
        _list.Visible = true;
        FitVideo();
    }

    protected override Control FocusTarget => _list;

    protected override void ApplyFont(Font font)
    {
        _list.Font = font;
        _renameBox.Font = font;
    }

    protected override IReadOnlyList<string> RestartArguments() => _folder is null ? [Dispatch.AxFile] : [Dispatch.AxFile, _folder];

    protected override string UpdateRestartText => $"closes {AxFileApp.Name}, installs it and starts it again on the same folder";

    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Folder: {_folder ?? "(none)"}",
        $"Player: {_player?.State.ToString() ?? "not started"}, speed {_settings.Speed}",
    ];

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
        if (_folder is { } closing && !FolderListing.IsThisPC(closing))
        {
            // AX-9.4: the folder joins the bundle's Recently closed list; AF-8.1: it is the last folder.
            RecentlyClosed.Remember(AxFileApp.Name, closing);
            _settings.LastFolder = closing;
        }

        // AF-5.5: closing the window stops the player.
        StopPlayer();
        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        // AF-4.7: while Windows' own menu for an entry shows, the shell fills its submenus through the owner's messages.
        if (Shell.ForwardMenuMessage(ref m))
        {
            return;
        }

        base.WndProc(ref m);
    }

    // ---- keys (AF-6.2): every key comes from BundleKeys or AxFileKeys, and is handled here only ----

    protected override bool ProcessWindowKey(Keys keyData)
    {
        switch (keyData)
        {
            case BundleKeys.Find:
                ShowFindNotice();
                return true;
            case BundleKeys.FindNext:
                FindInList(1);
                return true;
            case BundleKeys.FindPrevious:
                FindInList(-1);
                return true;
            case BundleKeys.Save:
                // The bundle's save key, with nothing here to save (AF-6.2).
                Announce("Nothing to save here", true);
                return true;
            case AxFileKeys.UpAlso:
                GoUp();
                return true;
            case AxFileKeys.NewWindow:
                NewWindow();
                return true;
            case AxFileKeys.PauseResume:
                PauseResume();
                return true;
            case AxFileKeys.Refresh:
                Do(() => RefreshAsync(announce: true));
                return true;
            case AxFileKeys.Filter:
                ShowFilterNotice();
                return true;
            case AxFileKeys.Favorite:
                ToggleFavorite();
                return true;
            case AxFileKeys.OpenFolder:
                OpenFolderDialog();
                return true;
            case AxFileKeys.GoToFolder:
                GoToFolder();
                return true;
            case AxFileKeys.ShowInExplorer:
                ShowFolderInExplorer();
                return true;
            case AxFileKeys.ShowItemInExplorer:
                ShowItemInExplorer();
                return true;
            case AxFileKeys.OpenInEditor:
                OpenInEditor();
                return true;
            case AxFileKeys.Information:
                SayInformation();
                return true;
            case AxFileKeys.Properties:
                ShowProperties();
                return true;
            case AxFileKeys.NewFolder:
                NewFolder();
                return true;
            case AxFileKeys.NewFile:
                NewFile();
                return true;
            case AxFileKeys.CopyPath:
                CopyPath();
                return true;
            case AxFileKeys.Forward:
                Seek(_settings.SeekSeconds);
                return true;
            case AxFileKeys.Back:
                Seek(-_settings.SeekSeconds);
                return true;
            case AxFileKeys.NextFile:
                MoveInQueue(1);
                return true;
            case AxFileKeys.PreviousFile:
                MoveInQueue(-1);
                return true;
            case AxFileKeys.GoToTime:
                GoToTime();
                return true;
            case AxFileKeys.Faster:
                ChangeSpeed(MediaFiles.Faster(_settings.Speed));
                return true;
            case AxFileKeys.Slower:
                ChangeSpeed(MediaFiles.Slower(_settings.Speed));
                return true;
            case AxFileKeys.WhatIsPlaying:
                SayWhatIsPlaying();
                return true;
        }

        if (_list.Focused)
        {
            // Enter, Backspace, Space, F2, Delete, the clipboard keys and the selection keys act in the list only (AF-6.2).
            switch (keyData)
            {
                case AxFileKeys.Cut:
                    CopyOrCut(cut: true);
                    return true;
                case AxFileKeys.Copy:
                    CopyOrCut(cut: false);
                    return true;
                case AxFileKeys.Paste:
                    Paste();
                    return true;
                case AxFileKeys.Select:
                    ToggleSelection();
                    return true;
                case AxFileKeys.SelectAll:
                    SelectAll();
                    return true;
                case AxFileKeys.SelectNone:
                    SelectNone();
                    return true;
                case AxFileKeys.ClearFilter:
                    return ClearFilter();
                case AxFileKeys.Open:
                    OpenSelected();
                    return true;
                case AxFileKeys.Up:
                    GoUp();
                    return true;
                case AxFileKeys.Rename:
                    Rename();
                    return true;
                case AxFileKeys.Delete:
                    Delete();
                    return true;
            }
        }

        return false;
    }

    /// <summary>Runs a task on the UI thread and turns an escaped exception into a notice rather than a crash.</summary>
    private async void Do(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error("AxFile command failed", ex);
            if (!IsDisposed)
            {
                Fail(ex.Message);
            }
        }
    }

    /// <summary>A failed operation is one announcement when the reason is one line, a notice otherwise (AF-4.5).</summary>
    private void Failed(string title, string reason)
    {
        if (reason.Contains('\n'))
        {
            ShowNotice(Notice.Plain(title, reason) with { Unprompted = true });
        }
        else
        {
            Announce("Failed: " + reason, true);
        }
    }

    private void UpdateStatusBar()
    {
        _stateLabel.Text = PlayerStatusText();
        _pathLabel.Text = _folder ?? string.Empty;
        StatusLayout.Fit(_status, _stateLabel, _pathLabel);
    }
}
